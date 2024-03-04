using System.Text.Json;
using EventStore.Client;
using Npgsql;
using Polly;
using Projac.Sql;
using Projac.Sql.Executors;
using Serilog;
using Transacto.Framework;
using Transacto.Framework.Projections;

namespace Transacto.Infrastructure.Npgsql;

public class NpgsqlProjectionHost : IHostedService {
	private readonly EventStoreClient _eventStore;
	private readonly IMessageTypeMapper _messageTypeMap;
	private readonly Func<NpgsqlConnection> _connectionFactory;
	private readonly NpgsqlProjection[] _projections;
	private readonly CancellationTokenSource _stopped;

	public NpgsqlProjectionHost(EventStoreClient eventStore, IMessageTypeMapper messageTypeMap,
		Func<NpgsqlConnection> connectionFactory, params NpgsqlProjection[] projections) {
		_eventStore = eventStore;
		_messageTypeMap = messageTypeMap;
		_connectionFactory = connectionFactory;
		_projections = projections;
		_stopped = new CancellationTokenSource();
	}

	public async Task StartAsync(CancellationToken cancellationToken) {
		if (_projections.Length == 0) {
			return;
		}

		await CreateSchema(cancellationToken);

		Subscribe();
	}

	private async void Subscribe() {
		var projectors = await ReadCheckpoints();
		
		var checkpoint = projectors.Select(p => p.Checkpoint).Min();
		
		await Policy.Handle<Exception>(ex => ex is not OperationCanceledException)
			.WaitAndRetryAsync(5, retryCount => TimeSpan.FromMilliseconds(retryCount * retryCount * 100))
			.ExecuteAsync(Subscribe);

		return;

		async Task Subscribe() {
			await using var subscription =
				_eventStore.SubscribeToAll(checkpoint, filterOptions: new(EventTypeFilter.ExcludeSystemEvents()));

			await foreach (var message in subscription.Messages) {
				if (message is not StreamMessage.Event(var resolvedEvent)) {
					continue;
				}

				if (!_messageTypeMap.TryMap(resolvedEvent.Event.EventType, out var type)) {
					continue;
				}

				var e = JsonSerializer.Deserialize(resolvedEvent.Event.Data.Span, type, TransactoSerializerOptions.Events)!;

				await Task.WhenAll(projectors
					.Where(projection =>
						projection.Checkpoint < FromAll.After(resolvedEvent.OriginalPosition ?? Position.Start))
					.Select(async projector => {
						await using var connection = _connectionFactory();
						await connection.OpenAsync(_stopped.Token);
						await using var transaction = await connection.BeginTransactionAsync(_stopped.Token);
						var (projection, _) = projector;
						var sqlProjector = new AsyncSqlProjector(projector.Resolver,
							new ConnectedTransactionalSqlCommandExecutor(transaction));
						await sqlProjector.ProjectAsync(e, _stopped.Token);
						await projection.WriteCheckpoint(transaction, resolvedEvent.Event.Position, _stopped.Token);
						await transaction.CommitAsync(_stopped.Token);
					}));

			}

		}
		
		async ValueTask<Projector[]> ReadCheckpoints() {
			await using var connection = _connectionFactory();
			await connection.OpenAsync(_stopped.Token);
			return await Task.WhenAll(Array.ConvertAll(_projections,
				async projection => new Projector(projection,
					await projection.ReadCheckpoint(connection, _stopped.Token))));
		}
	}

	private Task CreateSchema(CancellationToken cancellationToken) =>
		Task.WhenAll(_projections.Select(p =>
			new AsyncSqlProjector(Resolve.WhenEqualToHandlerMessageType(p),
				new NpgsqlExecutor(_connectionFactory)).ProjectAsync(new CreateSchema(), cancellationToken)));

	public Task StopAsync(CancellationToken cancellationToken) {
		_stopped.Cancel();
		return Task.CompletedTask;
	}

	private record Projector(NpgsqlProjection Projection, FromAll Checkpoint) {
		public SqlProjectionHandlerResolver Resolver { get; } = Resolve.WhenEqualToHandlerMessageType(Projection);
	}
}

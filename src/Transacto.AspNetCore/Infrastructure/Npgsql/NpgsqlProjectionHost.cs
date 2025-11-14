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

public class NpgsqlProjectionHost(
	EventStoreClient eventStore,
	IMessageTypeMapper messageTypeMap,
	Func<NpgsqlConnection> connectionFactory,
	params NpgsqlProjection[] projections)
	: IHostedService {
	private readonly CancellationTokenSource _stopped = new();

	public async Task StartAsync(CancellationToken cancellationToken) {
		if (projections.Length == 0) {
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
				eventStore.SubscribeToAll(checkpoint, filterOptions: new(EventTypeFilter.ExcludeSystemEvents()));

			await foreach (var message in subscription.Messages) {
				if (message is not StreamMessage.Event(var resolvedEvent)) {
					continue;
				}

				if (!messageTypeMap.TryMap(resolvedEvent.Event.EventType, out var type)) {
					continue;
				}

				var e = JsonSerializer.Deserialize(resolvedEvent.Event.Data.Span, type, TransactoSerializerOptions.Events)!;

				await Task.WhenAll(projectors
					.Where(projection =>
						projection.Checkpoint < FromAll.After(resolvedEvent.OriginalPosition ?? Position.Start))
					.Select(async projector => {
						await using var connection = connectionFactory();
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
			await using var connection = connectionFactory();
			await connection.OpenAsync(_stopped.Token);
			return await Task.WhenAll(Array.ConvertAll(projections,
				async projection => new Projector(projection,
					await projection.ReadCheckpoint(connection, _stopped.Token))));
		}
	}

	private Task CreateSchema(CancellationToken cancellationToken) =>
		Task.WhenAll(projections.Select(p =>
			new AsyncSqlProjector(Resolve.WhenEqualToHandlerMessageType(p),
				new NpgsqlExecutor(connectionFactory)).ProjectAsync(new CreateSchema(), cancellationToken)));

	public Task StopAsync(CancellationToken cancellationToken) {
		_stopped.Cancel();
		return Task.CompletedTask;
	}

	private record Projector(NpgsqlProjection Projection, FromAll Checkpoint) {
		public SqlProjectionHandlerResolver Resolver { get; } = Resolve.WhenEqualToHandlerMessageType(Projection);
	}
}

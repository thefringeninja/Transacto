using System.Text.Json;
using EventStore.Client;
using Npgsql;
using Polly;
using Projac.Sql;
using Projac.Sql.Executors;
using Transacto.Framework;
using Transacto.Framework.Projections;

namespace Transacto.Infrastructure.Npgsql;

public class NpgsqlProjectionHost(
	EventStoreClient eventStore,
	IMessageTypeMapper messageTypeMap,
	Func<NpgsqlConnection> connectionFactory,
	ILogger<NpgsqlProjectionHost> logger,
	params NpgsqlProjection[] projections)
	: BackgroundService {
	protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
		if (projections.Length == 0) {
			return;
		}

		await CreateSchema(stoppingToken);

		await Subscribe(stoppingToken);
	}

	private async Task Subscribe(CancellationToken ct) {
		try {
			await Policy.Handle<Exception>(ex => ex is not OperationCanceledException)
				.WaitAndRetryAsync(5, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
					(ex, delay) => logger.LogInformation(ex,
						"NpgsqlProjectionHost encountered an issue. Will retry after {delay}",
						delay))
				.ExecuteAsync(SubscribeInternal, ct);
		} catch (OperationCanceledException) { }
	}

	private async Task SubscribeInternal(CancellationToken ct) {
		var projectors = await ReadCheckpoints(ct);

		var checkpoint = projectors.Select(p => p.Checkpoint).Min();

		await using var subscription =
			eventStore.SubscribeToAll(checkpoint, filterOptions: new(EventTypeFilter.ExcludeSystemEvents()),
				cancellationToken: ct);

		await foreach (var message in subscription.Messages.WithCancellation(ct)) {
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
					await connection.OpenAsync(ct);
					await using var transaction = await connection.BeginTransactionAsync(ct);
					var (projection, _) = projector;
					var sqlProjector = new AsyncSqlProjector(projector.Resolver,
						new ConnectedTransactionalSqlCommandExecutor(transaction));
					await sqlProjector.ProjectAsync(e, ct);
					await projection.WriteCheckpoint(transaction, resolvedEvent.Event.Position, ct);
					await transaction.CommitAsync(ct);
				}));
		}
	}

	async ValueTask<Projector[]> ReadCheckpoints(CancellationToken ct) {
		await using var connection = connectionFactory();
		await connection.OpenAsync(ct);
		return await Task.WhenAll(Array.ConvertAll(projections,
			async projection => new Projector(projection,
				await projection.ReadCheckpoint(connection, ct))));
	}

	private Task CreateSchema(CancellationToken ct) =>
		Task.WhenAll(projections.Select(p =>
			new AsyncSqlProjector(Resolve.WhenEqualToHandlerMessageType(p),
				new NpgsqlExecutor(connectionFactory)).ProjectAsync(new CreateSchema(), ct)));

	private record Projector(NpgsqlProjection Projection, FromAll Checkpoint) {
		public SqlProjectionHandlerResolver Resolver { get; } = Resolve.WhenEqualToHandlerMessageType(Projection);
	}
}

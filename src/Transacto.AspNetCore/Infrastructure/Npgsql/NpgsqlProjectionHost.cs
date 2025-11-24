using System.Diagnostics;
using System.Text.Json;
using KurrentDB.Client;
using Npgsql;
using Polly;
using Projac.Sql;
using Projac.Sql.Executors;
using Transacto.Framework;
using Transacto.Framework.Projections;
using EventTypeFilter = KurrentDB.Client.EventTypeFilter;

namespace Transacto.Infrastructure.Npgsql;

public partial class NpgsqlProjectionHost(
	IPluginDescriptor plugin,
	KurrentDBClient eventStore,
	IMessageTypeMapper messageTypeMap,
	Func<NpgsqlConnection> connectionFactory,
	ILogger<NpgsqlProjectionHost> logger,
	params IEnumerable<NpgsqlProjection> projections)
	: BackgroundService {
	protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
		projections = projections.ToArray();
		if (!projections.Any()) {
			return;
		}

		await CreateSchema(stoppingToken);

		await Subscribe(stoppingToken);
	}

	private async Task Subscribe(CancellationToken ct) {
		try {
			await Policy.Handle<Exception>(ex => ex is not OperationCanceledException)
				.WaitAndRetryAsync(5, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
					(ex, delay) => LogWillRetryAfterDelay(logger, ex, plugin.Name, delay))
				.ExecuteAsync(SubscribeInternal, ct);
		} catch (OperationCanceledException) { }
	}

	private async Task SubscribeInternal(CancellationToken ct) {
		var scope = new NpgSqlProjectionScope(plugin.Name);

		var projectors = await LoadProjectors(ct);

		var checkpoint = projectors.Select(p => p.Checkpoint).Min();

		using (logger.BeginScope(scope)) {
			LogResumingFromCheckpoint(logger, checkpoint);
		}

		var stopwatch = Stopwatch.StartNew();

		await using var subscription =
			eventStore.SubscribeToAll(checkpoint, filterOptions: new(EventTypeFilter.ExcludeSystemEvents()),
				cancellationToken: ct);

		var batch = new List<ResolvedEvent>(110);


		await foreach (var message in subscription.Batch(100, TimeSpan.FromMilliseconds(20), ct)) {
			using var sps = logger.BeginScope(scope);

			switch (message) {
				case SubscriptionControlMessage.SubscriptionMessage (var streamMessage): {
					if (streamMessage is StreamMessage.CaughtUp) {
						stopwatch.Stop();
						LogCaughtUp(logger, stopwatch.Elapsed);
						continue;
					}

					if (streamMessage is not StreamMessage.Event(var resolvedEvent)) {
						continue;
					}

					using var ers = logger.BeginScope(Scopes.EventRecord.From(resolvedEvent.Event));

					if (!messageTypeMap.TryMap(resolvedEvent.Event.EventType, out _)) {
						LogDidNotUnderstandMessageType(logger);
						continue;
					}

					batch.Add(resolvedEvent);

					LogAddedToBatch(logger);
					break;
				}
				case SubscriptionControlMessage.Flush: {
					if (batch.Count == 0) {
						break;
					}

					LogFlushingBatch(logger, batch.Count);

					stopwatch.Restart();

					var messages = batch.Select(re => JsonSerializer.Deserialize(re.Event.Data.Span,
							messageTypeMap.Map(re.Event.EventType),
							TransactoSerializerOptions.Events) ?? throw new InvalidOperationException())
						.ToArray();

					await using var connection = connectionFactory();
					await connection.OpenAsync(ct);
					await using var transaction = await connection.BeginTransactionAsync(ct);

					var resolvedEvent = batch[0];
					var position = batch[^1].Event.Position;

					await Task.WhenAll(projectors
						.Where(projection =>
							projection.Checkpoint < FromAll.After(resolvedEvent.Event.Position))
						.Select(async projector => {
							var sqlProjector = new AsyncSqlProjector(
								projector.Resolver,
								new ConnectedTransactionalSqlCommandExecutor(transaction));
							await sqlProjector.ProjectAsync(messages, ct);
							await projector.Projection.WriteCheckpoint(transaction, position, ct);
						}));
					await transaction.CommitAsync(ct);
					batch.Clear();

					stopwatch.Stop();
					LogFlushedBatch(logger, position, batch.Count, stopwatch.Elapsed);
					break;
				}
			}
		}
	}

	async ValueTask<Projector[]> LoadProjectors(CancellationToken ct) {
		await using var connection = connectionFactory();
		await connection.OpenAsync(ct);
		return await Task.WhenAll(projections.Select(async projection => new Projector(projection,
			await projection.ReadCheckpoint(connection, ct))));
	}

	private Task CreateSchema(CancellationToken ct) =>
		Task.WhenAll(projections.Select(p =>
			new AsyncSqlProjector(Resolve.WhenEqualToHandlerMessageType(p),
				new NpgsqlExecutor(connectionFactory)).ProjectAsync(new CreateSchema(), ct)));

	private record Projector(NpgsqlProjection Projection, FromAll Checkpoint) {
		public SqlProjectionHandlerResolver Resolver { get; } = Resolve.WhenEqualToHandlerMessageType(Projection);
	}


	private readonly record struct NpgSqlProjectionScope(string Name);

	[LoggerMessage(LogLevel.Information,
		"NpgSqlProjection {npgSqlProjection} encountered an issue. Will retry after {delay}.")]
	static partial void LogWillRetryAfterDelay(ILogger<NpgsqlProjectionHost> logger, Exception ex,
		string npgSqlProjection, TimeSpan delay);

	[LoggerMessage(LogLevel.Trace, "Operation cancelled.")]
	static partial void LogOperationCancelled(ILogger<NpgsqlProjectionHost> logger);

	[LoggerMessage(LogLevel.Information, "NpgSqlProjection resuming from checkpoint {checkpoint}.")]
	static partial void LogResumingFromCheckpoint(ILogger<NpgsqlProjectionHost> logger, FromAll checkpoint);

	[LoggerMessage(LogLevel.Information, "NpgSqlProjection caught up in {elapsed}.")]
	static partial void LogCaughtUp(ILogger<NpgsqlProjectionHost> logger, TimeSpan elapsed);

	[LoggerMessage(LogLevel.Trace, "NpgSqlProjection saw {message}")]
	static partial void LogSawMessage(ILogger<NpgsqlProjectionHost> logger, StreamMessage message);

	[LoggerMessage(LogLevel.Debug, "NpgSqlProjection did not understand type.")]
	static partial void LogDidNotUnderstandMessageType(ILogger<NpgsqlProjectionHost> logger);

	[LoggerMessage(LogLevel.Debug, "NpgSqlProjection added message to batch.")]
	static partial void LogAddedToBatch(ILogger<NpgsqlProjectionHost> logger);

	[LoggerMessage(LogLevel.Debug, "NpgSqlProjection flushing batch with {count} messages.")]
	static partial void LogFlushingBatch(ILogger<NpgsqlProjectionHost> logger, int count);

	[LoggerMessage(LogLevel.Debug,
		"NpgSqlProjection flushed batch at checkpoint {checkpoint} with {count} messages in {elapsed}.")]
	static partial void LogFlushedBatch(ILogger<NpgsqlProjectionHost> logger, Position checkpoint, int count,
		TimeSpan elapsed);

	[LoggerMessage(LogLevel.Information, "Schema created {projection}")]
	static partial void LogSchemaCreated(ILogger<NpgsqlProjectionHost> logger, string projection);
}

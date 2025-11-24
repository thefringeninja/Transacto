using System.Data.SQLite;
using System.Diagnostics;
using System.Text.Json;
using KurrentDB.Client;
using Polly;
using Projac.Sql;
using Projac.Sql.Executors;
using Projac.SQLite;
using Transacto.Framework;
using Transacto.Framework.Projections;
using EventTypeFilter = KurrentDB.Client.EventTypeFilter;

namespace Transacto.Infrastructure.SQLite;

public partial class SQLiteProjectionHost(
	KurrentDBClient eventStore,
	IMessageTypeMapper messageTypeMap,
	Func<SQLiteConnection> connectionFactory,
	SQLiteProjection projection,
	ILogger<SQLiteProjectionHost> logger)
	: BackgroundService {
	private readonly SqlProjectionHandlerResolver _resolver = Resolve.WhenEqualToHandlerMessageType(projection);

	private static readonly SQLiteSyntax Sql = new();

	protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
		CreateSchema();

		await Subscribe(stoppingToken);
	}

	private async Task Subscribe(CancellationToken ct) {
		try {
			await Policy.Handle<Exception>(ex => ex is not OperationCanceledException)
				.WaitAndRetryAsync(5, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
					(ex, delay) =>
						LogWillRetryAfterDelay(logger, ex, projection.Name,
							delay))
				.ExecuteAsync(SubscribeInternal, ct);
		} catch (OperationCanceledException) { }
	}

	async Task SubscribeInternal(CancellationToken ct) {
		var scope = new SQLiteProjectionScope(projection.Name);

		var checkpoint = ReadCheckpoint();

		using (logger.BeginScope(scope)) {
			LogResumingFromCheckpoint(logger, checkpoint);
		}

		var stopwatch = Stopwatch.StartNew();

		await using var subscription = eventStore.SubscribeToAll(checkpoint,
			filterOptions: new(EventTypeFilter.ExcludeSystemEvents()), cancellationToken: ct);

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
					await using var connection = connectionFactory();
					connection.Open();
					await using var transaction = connection.BeginTransaction();
					var sqlProjector = new SqlProjector(
						_resolver, new ConnectedTransactionalSqlCommandExecutor(transaction));
					var messages = batch.Select(re => JsonSerializer.Deserialize(re.Event.Data.Span,
							messageTypeMap.Map(re.Event.EventType),
							TransactoSerializerOptions.Events) ?? throw new InvalidOperationException())
						.ToArray();
					sqlProjector.Project(messages);
					var position = batch[^1].Event.Position;
					WriteCheckpoint(transaction, position);
					transaction.Commit();
					batch.Clear();

					stopwatch.Stop();
					LogFlushedBatch(logger, position, batch.Count, stopwatch.Elapsed);
					break;
				}
			}
		}
	}

	private void CreateSchema() {
		using (logger.BeginScope(new SQLiteProjectionScope(projection.Name))) {
			var executor = new SQLiteExecutor(connectionFactory);

			executor.ExecuteNonQuery(Sql.NonQueryStatement(SQLiteScripts.CreateSchema));

			new SqlProjector(_resolver, executor).Project(new CreateSchema());

			LogSchemaCreated(logger, projection.Name);
		}
	}

	private FromAll ReadCheckpoint() {
		using var connection = connectionFactory();
		connection.Open();

		var statement = Sql.QueryStatement(SQLiteScripts.ReadCheckpoint);

		using var command = new SQLiteCommand(statement.Text, connection);

		using var reader = command.ExecuteReader();
		return !reader.Read()
			? FromAll.Start
			: FromAll.After(new Position(unchecked((ulong)reader.GetInt64(0)), unchecked((ulong)reader.GetInt64(1))));
	}

	private static void WriteCheckpoint(SQLiteTransaction transaction, Position position) {
		var statement = Sql.QueryStatement(SQLiteScripts.WriteCheckpoint);

		using var command = unchecked(new SQLiteCommand(statement.Text, transaction.Connection, transaction) {
			Parameters = {
				Sql.Int64((long)position.CommitPosition).ToDbParameter("commit"),
				Sql.Int64((long)position.PreparePosition).ToDbParameter("prepare"),
			}
		});

		command.ExecuteNonQuery();
	}


	private readonly record struct SQLiteProjectionScope(string Name);

	[LoggerMessage(LogLevel.Information,
		"SQLiteProjection {SQLiteProjection} encountered an issue. Will retry after {delay}.")]
	static partial void LogWillRetryAfterDelay(ILogger<SQLiteProjectionHost> logger, Exception ex,
		string SQLiteProjection, TimeSpan delay);

	[LoggerMessage(LogLevel.Trace, "Operation cancelled.")]
	static partial void LogOperationCancelled(ILogger<SQLiteProjectionHost> logger);

	[LoggerMessage(LogLevel.Information, "SQLiteProjection resuming from checkpoint {checkpoint}.")]
	static partial void LogResumingFromCheckpoint(ILogger<SQLiteProjectionHost> logger, FromAll checkpoint);

	[LoggerMessage(LogLevel.Information, "SQLiteProjection caught up in {elapsed}.")]
	static partial void LogCaughtUp(ILogger<SQLiteProjectionHost> logger, TimeSpan elapsed);

	[LoggerMessage(LogLevel.Trace, "SQLiteProjection saw {message}")]
	static partial void LogSawMessage(ILogger<SQLiteProjectionHost> logger, StreamMessage message);

	[LoggerMessage(LogLevel.Debug, "SQLiteProjection did not understand type.")]
	static partial void LogDidNotUnderstandMessageType(ILogger<SQLiteProjectionHost> logger);

	[LoggerMessage(LogLevel.Debug, "SQLiteProjection added message to batch.")]
	static partial void LogAddedToBatch(ILogger<SQLiteProjectionHost> logger);

	[LoggerMessage(LogLevel.Debug, "SQLiteProjection flushing batch with {count} messages.")]
	static partial void LogFlushingBatch(ILogger<SQLiteProjectionHost> logger, int count);

	[LoggerMessage(LogLevel.Debug,
		"SQLiteProjection flushed batch at checkpoint {checkpoint} with {count} messages in {elapsed}.")]
	static partial void LogFlushedBatch(ILogger<SQLiteProjectionHost> logger, Position checkpoint, int count,
		TimeSpan elapsed);

	[LoggerMessage(LogLevel.Information, "Schema created {projection}")]
	static partial void LogSchemaCreated(ILogger<SQLiteProjectionHost> logger, string projection);
}

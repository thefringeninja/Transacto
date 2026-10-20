using System.Data.SQLite;
using System.Diagnostics;
using System.Text.Json;
using KurrentDB.Client;
using Polly;
using Projac.Sql;
using Projac.Sql.Executors;
using Projac.SQLite;
using Transacto.Framework;
using Transacto.Framework.Logging;
using Transacto.Framework.Projections;
using EventTypeFilter = KurrentDB.Client.EventTypeFilter;

namespace Transacto.Infrastructure.SQLite;

public partial class SQLiteProjectionHost<T>(
	KurrentDBClient eventStore,
	IMessageTypeMapper messageTypeMap,
	Func<SQLiteConnection> connectionFactory,
	IPluginDescriptor plugin,
	SQLiteProjection projection,
	ILogger<SQLiteProjectionHost<T>> logger)
	: BackgroundService, IProjectionsControl, IAsyncDisposable where T : SqlProjection {
	private readonly SqlProjectionHandlerResolver _resolver = Resolve.WhenEqualToHandlerMessageType(projection);

	private readonly Scopes.Plugins _pluginScope = new(plugin.Name);
	private readonly Scopes.Projections _projectionsScope = new(projection.Name);


	private ProjectionState _state = ProjectionState.Named(projection.Name);
	private KurrentDBClient.StreamSubscriptionResult? _subscription;
	private TaskCompletionSource _pauseSubscription = new();
	private readonly SQLiteExecutor _sqlExecutor = new(connectionFactory);
	private SqlProjector sqlProjector => field ??= new SqlProjector(_resolver, _sqlExecutor);

	private static readonly SQLiteSyntax Sql = new();

	public IReadOnlyList<ProjectionState> State => [_state];

	public bool TryGetState(string name, out ProjectionState state) {
		if (name != _state.Name) {
			state = default;
			return false;
		}

		state = _state;
		return true;
	}

	public async Task Delete(string name) {
		_pauseSubscription = new();
		try {
			if (_subscription is { } subscription) {
				await subscription.DisposeAsync();
			}

			DropSchema();
			CreateSchema();
		} finally {
			_pauseSubscription.SetResult();
		}
	}

	public event Action<object, EventArgs>? CaughtUp;

	public async ValueTask DisposeAsync() {
		if (_subscription is not { } s) {
			return;
		}

		await s.DisposeAsync();
	}

	public override void Dispose() {
		_subscription?.Dispose();
		base.Dispose();
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
		using (logger.BeginScope(_pluginScope))
		using (logger.BeginScope(_projectionsScope)) {
			CreateSchema();
		}

		_pauseSubscription.SetResult();
		await Subscribe(stoppingToken);
	}

	private async Task Subscribe(CancellationToken ct) {
		try {
			await Policy.WrapAsync(ProjectionPolicy.CircuitBreaker, ProjectionPolicy.RetryExponentially(OnRetry))
				.ExecuteAsync(SubscribeInternal, ct);

			void OnRetry(Exception ex, TimeSpan delay) {
				_state = _state with {
					Status = ProjectionStatus.Faulted,
					ErrorMessage = ex.Message
				};
				LogWillRetryAfterDelay(logger, ex, delay);
			}
		} catch (OperationCanceledException) { }
	}

	private async Task SubscribeInternal(CancellationToken ct) {
		await _pauseSubscription.Task;
		using var ps = logger.BeginScope(_pluginScope);
		using var pps = logger.BeginScope(_projectionsScope);

		var checkpoint = ReadCheckpoint();

		_state = _state.Checkpoint(checkpoint);

		await FellBehind(ct);

		LogResumingFromCheckpoint(logger, checkpoint);

		var stopwatch = Stopwatch.StartNew();

		_subscription = eventStore.SubscribeToAll(checkpoint,
			filterOptions: new(EventTypeFilter.ExcludeSystemEvents()), cancellationToken: ct);

		var batch = new List<ResolvedEvent>(110);

		await foreach (var message in _subscription.Batch(100, TimeSpan.FromMilliseconds(20), ct)) {
			switch (message) {
				case SubscriptionControlMessage.SubscriptionMessage (var streamMessage): {
					if (streamMessage is StreamMessage.CaughtUp) {
						stopwatch.Stop();
						LogCaughtUp(logger, stopwatch.Elapsed);

						_state = _state with { Status = ProjectionStatus.Live };
						
						CaughtUp?.Invoke(this, EventArgs.Empty);
						continue;
					}

					if (streamMessage is StreamMessage.FellBehind) {
						await FellBehind(ct);
						continue;
					}

					if (streamMessage is not StreamMessage.Event(var resolvedEvent)) {
						continue;
					}

					_state = _state.Checkpoint(FromAll.After(resolvedEvent.Event.Position));

					using var ers = logger.BeginScope(Scopes.EventRecord.From(resolvedEvent.Event));

					if (!messageTypeMap.TryMap(resolvedEvent.Event.EventType, out _)) {
						LogDidNotUnderstandMessageType(logger);
						continue;
					}

					batch.Add(resolvedEvent);

					LogAddedToBatch(logger);
					continue;
				}
				case SubscriptionControlMessage.Flush: {
					var batchCount = batch.Count;	
					
					if (batchCount == 0) {
						continue;
					}

					LogFlushingBatch(logger, batchCount);

					stopwatch.Restart();
					await using var connection = connectionFactory();
					connection.Open();
					await using var transaction = connection.BeginTransaction();

					new SqlProjector(_resolver, new ConnectedTransactionalSqlCommandExecutor(transaction))
						.Project(batch.Select(re => JsonSerializer.Deserialize(
							re.Event.Data.Span,
							messageTypeMap.Map(re.Event.EventType),
							TransactoSerializerOptions.Messages) ?? throw new InvalidOperationException()));

					var position = batch[^1].Event.Position;
					WriteCheckpoint(transaction, position);
					transaction.Commit();

					batch.Clear();

					await UpdateHead(ct);

					stopwatch.Stop();
					LogFlushedBatch(logger, position, batchCount, stopwatch.Elapsed);
					
					continue;
				}
			}
		}
	}

	private void CreateSchema() {
		_sqlExecutor.ExecuteNonQuery(Sql.NonQueryStatement(SQLiteScripts.CreateSchema));

		sqlProjector.Project(new CreateSchema());

		LogSchemaCreated(logger);
	}

	private void DropSchema() {
		sqlProjector.Project(new DropSchema());

		LogSchemaCreated(logger);
	}


	private FromAll ReadCheckpoint() {
		using var connection = connectionFactory();
		connection.Open();

		var statement = Sql.QueryStatement(SQLiteScripts.ReadCheckpoint);

		using var command = new SQLiteCommand(statement.Text, connection);

		using var reader = command.ExecuteReader();
		if (!reader.Read()) {
			return FromAll.Start;
		}

		var position = new Position(unchecked((ulong)reader.GetInt64(0)), unchecked((ulong)reader.GetInt64(1)));

		return position == Position.Start ? FromAll.Start : FromAll.After(position);
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

	private async Task FellBehind(CancellationToken ct) {
		await UpdateHead(ct);
		_state = _state with { Status = ProjectionStatus.CatchingUp };
	}

	private async Task UpdateHead(CancellationToken ct) {
		var last = await eventStore.ReadAllAsync(Direction.Backwards, Position.End, 1)
			.Select(re => re.Event.Position)
			.FirstOrDefaultAsync(ct);

		_state = _state.End(FromAll.After(last));
	}

	[LoggerMessage(LogLevel.Information,
		"SQLiteProjection encountered an issue. Will retry after {delay}.")]
	static partial void LogWillRetryAfterDelay(ILogger<SQLiteProjectionHost<T>> logger, Exception ex, TimeSpan delay);

	[LoggerMessage(LogLevel.Trace, "Operation cancelled.")]
	static partial void LogOperationCancelled(ILogger<SQLiteProjectionHost<T>> logger);

	[LoggerMessage(LogLevel.Information, "SQLiteProjection resuming from checkpoint {checkpoint}.")]
	static partial void LogResumingFromCheckpoint(ILogger<SQLiteProjectionHost<T>> logger, FromAll checkpoint);

	[LoggerMessage(LogLevel.Information, "SQLiteProjection caught up in {elapsed}.")]
	static partial void LogCaughtUp(ILogger<SQLiteProjectionHost<T>> logger, TimeSpan elapsed);

	[LoggerMessage(LogLevel.Trace, "SQLiteProjection saw {message}")]
	static partial void LogSawMessage(ILogger<SQLiteProjectionHost<T>> logger, StreamMessage message);

	[LoggerMessage(LogLevel.Debug, "SQLiteProjection did not understand type.")]
	static partial void LogDidNotUnderstandMessageType(ILogger<SQLiteProjectionHost<T>> logger);

	[LoggerMessage(LogLevel.Debug, "SQLiteProjection added message to batch.")]
	static partial void LogAddedToBatch(ILogger<SQLiteProjectionHost<T>> logger);

	[LoggerMessage(LogLevel.Debug, "SQLiteProjection flushing batch with {count} messages.")]
	static partial void LogFlushingBatch(ILogger<SQLiteProjectionHost<T>> logger, int count);

	[LoggerMessage(LogLevel.Debug,
		"SQLiteProjection flushed batch at checkpoint {checkpoint} with {count} messages in {elapsed}.")]
	static partial void LogFlushedBatch(ILogger<SQLiteProjectionHost<T>> logger, Position checkpoint, int count,
		TimeSpan elapsed);

	[LoggerMessage(LogLevel.Information, "SQLiteProjection schema created.")]
	static partial void LogSchemaCreated(ILogger<SQLiteProjectionHost<T>> logger);
}

using System.Collections.Concurrent;
using System.Data;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using KurrentDB.Client;
using Npgsql;
using Polly;
using Projac.Sql;
using Projac.Sql.Executors;
using Transacto.Framework;
using Transacto.Framework.Logging;
using Transacto.Framework.Projections;
using EventTypeFilter = KurrentDB.Client.EventTypeFilter;

namespace Transacto.Infrastructure.Npgsql;

public partial class NpgsqlProjectionHost(
	IPluginDescriptor plugin,
	KurrentDBClient eventStore,
	IMessageTypeMapper messageTypeMap,
	NpgsqlDataSource dataSource,
	ILogger<NpgsqlProjectionHost> logger,
	params IEnumerable<NpgsqlProjection> projections)
	: BackgroundService, IProjectionsControl {
	private readonly NpgsqlProjection[] _projections = projections.ToArray();

	private readonly ConcurrentDictionary<string, ProjectionState> _state = [];
	private readonly Scopes.Plugins _loggingScope = new(plugin.Name);

	public IReadOnlyList<ProjectionState> State => _state.Values.ToList();

	public bool TryGetState(string name, out ProjectionState state) =>
		_state.TryGetValue(name, out state);

	public Task Delete(string name) {
		throw new NotImplementedException();
	}
	
	public event Action<object, EventArgs>? CaughtUp;

	protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
		using (var sps = logger.BeginScope(_loggingScope)) {
			if (_projections.Length == 0) {
				logger.LogInformation("No projections registered.");
				return;
			}

			logger.LogInformation("{count} projections registered.", _projections.Length);
		}

		await CreateSchema(stoppingToken);

		await Subscribe(stoppingToken);
	}

	private async Task Subscribe(CancellationToken ct) {
		try {
			await Policy.WrapAsync(ProjectionPolicy.CircuitBreaker, ProjectionPolicy.RetryExponentially(OnRetry))
				.ExecuteAsync(SubscribeInternal, ct);

			void OnRetry(Exception ex, TimeSpan delay) {
				_state.UpdateAll(static (state, ex) => state with {
					Status = ProjectionStatus.Faulted,
					ErrorMessage = ex.Message
				}, ex);
				LogWillRetryAfterDelay(logger, ex, plugin.Name, delay);
			}
		} catch (OperationCanceledException) { }
	}

	private async Task SubscribeInternal(CancellationToken ct) {
		FromAll checkpoint = FromAll.End;
		var projectors = await LoadProjectors(ct).ToArrayAsync(ct);

		foreach (var projector in projectors) {
			checkpoint = checkpoint < projector.Checkpoint ? checkpoint : projector.Checkpoint;
			var key = projector.Projection.GetType().Name;
			_state[key] = ProjectionState.Named(key).Checkpoint(projector.Checkpoint);
		}

		using (logger.BeginScope(_loggingScope)) {
			LogResumingFromCheckpoint(logger, checkpoint);
		}

		await FellBehind(ct);

		var stopwatch = Stopwatch.StartNew();

		await using var subscription =
			eventStore.SubscribeToAll(checkpoint, filterOptions: new(EventTypeFilter.ExcludeSystemEvents()),
				cancellationToken: ct);

		var batch = new List<ResolvedEvent>(110);

		await foreach (var message in subscription.Batch(100, TimeSpan.FromMilliseconds(20), ct)) {
			using var sps = logger.BeginScope(_loggingScope);

			switch (message) {
				case SubscriptionControlMessage.SubscriptionMessage (var streamMessage): {
					switch (streamMessage) {
						case StreamMessage.CaughtUp: {
							stopwatch.Stop();
							LogCaughtUp(logger, stopwatch.Elapsed);

							_state.UpdateAll(value => value with { Status = ProjectionStatus.Live });

							CaughtUp?.Invoke(this, EventArgs.Empty);
							continue;
						}
						case StreamMessage.FellBehind: {
							await FellBehind(ct);

							continue;
						}
					}

					if (streamMessage is not StreamMessage.Event(var resolvedEvent)) {
						continue;
					}

					_state.UpdateAll((value, position) => value
						.End(FromAll.After(position))
						.Checkpoint(FromAll.After(position)), resolvedEvent.Event.Position);

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

					var messages = batch.Select(re => JsonSerializer.Deserialize(re.Event.Data.Span,
							messageTypeMap.Map(re.Event.EventType),
							TransactoSerializerOptions.Messages) ?? throw new InvalidOperationException())
						.ToArray();

					var resolvedEvent = batch[0];
					var position = batch[^1].Event.Position;

					await using var connection = await dataSource.OpenConnectionAsync(ct);
					await using var transaction = await connection.BeginTransactionAsync(ct);
					var executor = new ConnectedTransactionalSqlCommandExecutor(transaction);

					var fromAll = FromAll.After(resolvedEvent.Event.Position);
					foreach (var projector in projectors) {
						if (projector.Checkpoint > fromAll) {
							continue;
						}

						var sqlProjector = new AsyncSqlProjector(projector.Resolver, executor);
						await sqlProjector.ProjectAsync(messages, ct);
						await projector.Projection.WriteCheckpoint(transaction, position, ct);
					}

					await transaction.CommitAsync(ct);
					batch.Clear();

					stopwatch.Stop();
					LogFlushedBatch(logger, position, batchCount, stopwatch.Elapsed);

					continue;
				}
			}
		}
	}

	private async Task FellBehind(CancellationToken ct) {
		var last = await eventStore.ReadAllAsync(Direction.Backwards, Position.End, 1)
			.Select(re => re.Event.Position)
			.FirstOrDefaultAsync(ct);

		_state.UpdateAll(
			static (value, position) =>
				value.End(FromAll.After(position)) with { Status = ProjectionStatus.CatchingUp },
			last);
	}

	async IAsyncEnumerable<Projector> LoadProjectors([EnumeratorCancellation] CancellationToken ct) {
		await using var connection = await dataSource.OpenConnectionAsync(ct);
		foreach (var projection in _projections) {
			var checkpoint = await projection.ReadCheckpoint(connection, ct);
			yield return new Projector(projection, checkpoint);
		}
	}

	private async Task CreateSchema(CancellationToken ct) {
		await using var connection = await dataSource.OpenConnectionAsync(ct);
		await using var transaction = await connection.BeginTransactionAsync(ct);

		var executor = new ConnectedTransactionalSqlCommandExecutor(transaction);

		await executor.ExecuteNonQueryAsync(
			new SqlNonQueryCommand(NpgsqlScripts.CreateCheckpoints, [], CommandType.Text), ct);

		foreach (var projection in _projections) {
			var projector = new AsyncSqlProjector(Resolve.WhenAssignableToHandlerMessageType(projection), executor);
			await projector.ProjectAsync(new CreateSchema(), ct);
		}

		await transaction.CommitAsync(ct);
	}

	private record Projector(NpgsqlProjection Projection, FromAll Checkpoint) {
		public SqlProjectionHandlerResolver Resolver { get; } = Resolve.WhenEqualToHandlerMessageType(Projection);
	}

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

	[LoggerMessage(LogLevel.Information, "NpgSqlProjection schema created {projection}")]
	static partial void LogSchemaCreated(ILogger<NpgsqlProjectionHost> logger, string projection);

}

using System.Diagnostics;
using System.Text.Json;
using KurrentDB.Client;
using Polly;
using Projac;
using SqlStreamStore;
using Transacto.Framework;
using Transacto.Framework.Logging;
using Transacto.Framework.Projections;
using EventTypeFilter = KurrentDB.Client.EventTypeFilter;

namespace Transacto.Infrastructure.SqlStreamStore;

public partial class StreamStoreProjectionHost(
	KurrentDBClient eventStore,
	IMessageTypeMapper messageTypeMap,
	IStreamStore streamStore,
	IPluginDescriptor plugin,
	ILogger<StreamStoreProjectionHost> logger,
	params IEnumerable<StreamStoreProjection> projections)
	: BackgroundService, IProjectionsControl {
	public string Name { get; } = $"[{string.Join(',', projections.Select(x => x.StreamName))}]";

	private readonly Scopes.Plugins _pluginScope = new(plugin.Name);

	protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
		try {
			await Policy.WrapAsync(ProjectionPolicy.CircuitBreaker, ProjectionPolicy.RetryExponentially(OnRetry))
				.ExecuteAsync(SubscribeInternal, stoppingToken);

			void OnRetry(Exception ex, TimeSpan delay) {
				LogWillRetryAfterDelay(logger, ex, Name, delay);
			}
		} catch (OperationCanceledException) { }
	}

	private async Task SubscribeInternal(CancellationToken ct) {
		if (!projections.Any()) {
			return;
		}

		var projectors = await GetProjectors();
		var checkpoint = projectors.Select(x => x.Checkpoint).Min();

		LogResumingFromCheckpoint(logger, checkpoint);

		await using var subscription = eventStore.SubscribeToAll(checkpoint,
			filterOptions: new SubscriptionFilterOptions(EventTypeFilter.ExcludeSystemEvents()));

		var stopwatch = Stopwatch.StartNew();

		await foreach (var message in subscription.Messages.WithCancellation(ct)) {
			using (logger.BeginScope(_pluginScope)) {
				LogSawMessage(logger, message);
				switch (message) {
					case StreamMessage.CaughtUp:
						stopwatch.Stop();
						LogCaughtUp(logger, stopwatch.Elapsed);
						CaughtUp?.Invoke(this, EventArgs.Empty);
						break;
					case StreamMessage.FellBehind:

						continue;
				}

				if (message is not StreamMessage.Event(var resolvedEvent)) {
					continue;
				}

				var er = resolvedEvent.Event;

				using var ers = logger.BeginScope(Scopes.EventRecord.From(er));

				if (!messageTypeMap.TryMap(er.EventType, out var type)) {
					LogDidNotUnderstandMessageType(logger, er.EventType);
					continue;
				}

				var e = JsonSerializer.Deserialize(er.Data.Span, type!, TransactoSerializerOptions.Messages);
				ArgumentNullException.ThrowIfNull(e);

				await Task.WhenAll(projectors
					.Where(x => x.Checkpoint < FromAll.After(er.Position))
					.Select(ProtectAsync(e, er.Position)));
			}
		}

		return;

		Task<CheckpointedProjector[]> GetProjectors() => Task.WhenAll(projections.Select(LoadProjector));

		Func<CheckpointedProjector, Task> ProtectAsync(object e, Position streamPosition) => async cp => {
			using var scope = logger.BeginScope(new Scopes.Projections(cp.Projection.StreamName));
			try {
				await cp.Projector.ProjectAsync(streamStore, new Envelope(e, streamPosition), ct);
			} catch (Exception ex) {
				LogFailedToApplyMessage(logger, ex, e);
				throw;
			}
		};

		async Task<CheckpointedProjector> LoadProjector(StreamStoreProjection projection) {
			var checkpoint = await projection.ReadCheckpoint(streamStore, ct);
			var projector = new Projector<IStreamStore>(Resolve.WhenAssignableToHandlerMessageType(projection.Handlers));

			return new(projector, checkpoint, projection);
		}
	}

	private record CheckpointedProjector(Projector<IStreamStore> Projector, FromAll Checkpoint, StreamStoreProjection Projection);

	
	public IReadOnlyList<ProjectionState> State { get; } = [];

	public bool TryGetState(string name, out ProjectionState state) {
		throw new NotImplementedException();
	}

	public Task Delete(string name) {
		throw new NotImplementedException();
	}

	public event Action<object, EventArgs>? CaughtUp;

	[LoggerMessage(LogLevel.Information,
		"StreamStoreProjection {StreamStoreProjection} encountered an issue. Will retry after {delay}.")]
	static partial void LogWillRetryAfterDelay(ILogger<StreamStoreProjectionHost> logger, Exception ex,
		string StreamStoreProjection, TimeSpan delay);

	[LoggerMessage(LogLevel.Trace, "Operation cancelled.")]
	static partial void LogOperationCancelled(ILogger<StreamStoreProjectionHost> logger);

	[LoggerMessage(LogLevel.Information, "StreamStoreProjection resuming from checkpoint {checkpoint}.")]
	static partial void LogResumingFromCheckpoint(ILogger<StreamStoreProjectionHost> logger, FromAll checkpoint);

	[LoggerMessage(LogLevel.Information, "StreamStoreProjection caught up in {elapsed}.")]
	static partial void LogCaughtUp(ILogger<StreamStoreProjectionHost> logger, TimeSpan elapsed);

	[LoggerMessage(LogLevel.Trace, "StreamStoreProjection saw {message}")]
	static partial void LogSawMessage(ILogger<StreamStoreProjectionHost> logger, StreamMessage message);

	[LoggerMessage(LogLevel.Debug, "StreamStoreProjection did not understand type '{eventType}.")]
	static partial void LogDidNotUnderstandMessageType(ILogger<StreamStoreProjectionHost> logger, string eventType);

	[LoggerMessage(LogLevel.Debug, "StreamStoreProjection failed to apply message @{message}")]
	static partial void LogFailedToApplyMessage(ILogger<StreamStoreProjectionHost> logger, Exception ex, object message);

	[LoggerMessage(LogLevel.Trace, "StreamStoreProjection added message to batch.")]
	static partial void LogAddedToBatch(ILogger<StreamStoreProjectionHost> logger);

	[LoggerMessage(LogLevel.Trace, "StreamStoreProjection flushing batch with {count} messages.")]
	static partial void LogFlushingBatch(ILogger<StreamStoreProjectionHost> logger, int count);

	[LoggerMessage(LogLevel.Debug,
		"StreamStoreProjection flushed batch at checkpoint {checkpoint} with {count} messages in {elapsed}.")]
	static partial void LogFlushedBatch(ILogger<StreamStoreProjectionHost> logger, Position checkpoint, int count,
		TimeSpan elapsed);

	[LoggerMessage(LogLevel.Information, "Schema created {projection}")]
	static partial void LogSchemaCreated(ILogger<StreamStoreProjectionHost> logger, string projection);
}

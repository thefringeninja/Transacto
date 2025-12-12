using System.Text.Json;
using KurrentDB.Client;
using Polly;
using Projac;
using SqlStreamStore;
using Transacto.Framework;
using Transacto.Framework.Projections;

namespace Transacto.Infrastructure.SqlStreamStore;

public partial class StreamStoreProjectionHost(
	KurrentDBClient eventStore,
	IMessageTypeMapper messageTypeMap,
	IStreamStore streamStore,
	ILogger<StreamStoreProjectionHost> logger,
	params IEnumerable<StreamStoreProjection> projections)
	: BackgroundService, IProjectionsControl {
	public string Name { get; } = $"[{string.Join(',', projections.Select(x => x.StreamName))}]";
	
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

		await using var subscription = eventStore.SubscribeToAll(checkpoint,
			filterOptions: new SubscriptionFilterOptions(EventTypeFilter.ExcludeSystemEvents()));

		await foreach (var message in subscription.Messages.WithCancellation(ct)) {
			if (message is not StreamMessage.Event(var resolvedEvent)) {
				continue;
			}

			if (!messageTypeMap.TryMap(resolvedEvent.Event.EventType, out var type)) {
				continue;
			}

			var e = JsonSerializer.Deserialize(resolvedEvent.Event.Data.Span, type!,
				TransactoSerializerOptions.Events)!;

			await Task.WhenAll(projectors
				.Where(x => x.Checkpoint < FromAll.After(resolvedEvent.OriginalPosition ?? Position.Start))
				.Select(_ => _.Projector.ProjectAsync(streamStore,
					new Envelope(message, resolvedEvent.OriginalEvent.Position), ct)));
		}

		Task<CheckpointedProjector[]> GetProjectors() => Task.WhenAll(projections.Select(async projection =>
			new CheckpointedProjector(
				new Projector<IStreamStore>(Resolve.WhenAssignableToHandlerMessageType(projection.Handlers)),
				await projection.ReadCheckpoint(streamStore, ct))));
	}

	private record CheckpointedProjector(Projector<IStreamStore> Projector, FromAll Checkpoint);

	public IReadOnlyList<ProjectionState> State { get; } = [];

	public bool TryGetState(string name, out ProjectionState state) {
		throw new NotImplementedException();
	}

	public Task Delete(string name) {
		throw new NotImplementedException();
	}

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

	[LoggerMessage(LogLevel.Debug, "StreamStoreProjection did not understand type.")]
	static partial void LogDidNotUnderstandMessageType(ILogger<StreamStoreProjectionHost> logger);

	[LoggerMessage(LogLevel.Debug, "StreamStoreProjection added message to batch.")]
	static partial void LogAddedToBatch(ILogger<StreamStoreProjectionHost> logger);

	[LoggerMessage(LogLevel.Debug, "StreamStoreProjection flushing batch with {count} messages.")]
	static partial void LogFlushingBatch(ILogger<StreamStoreProjectionHost> logger, int count);

	[LoggerMessage(LogLevel.Debug,
		"StreamStoreProjection flushed batch at checkpoint {checkpoint} with {count} messages in {elapsed}.")]
	static partial void LogFlushedBatch(ILogger<StreamStoreProjectionHost> logger, Position checkpoint, int count,
		TimeSpan elapsed);

	[LoggerMessage(LogLevel.Information, "Schema created {projection}")]
	static partial void LogSchemaCreated(ILogger<StreamStoreProjectionHost> logger, string projection);
}

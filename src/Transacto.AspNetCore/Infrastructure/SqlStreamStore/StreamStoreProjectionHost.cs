using System.Text.Json;
using EventStore.Client;
using Polly;
using Projac;
using SqlStreamStore;
using Transacto.Framework;

namespace Transacto.Infrastructure.SqlStreamStore;

public class StreamStoreProjectionHost : IHostedService {
	private readonly EventStoreClient _eventStore;
	private readonly IMessageTypeMapper _messageTypeMap;
	private readonly IStreamStore _streamStore;
	private readonly StreamStoreProjection[] _projections;
	private readonly CancellationTokenSource _stopped;

	public StreamStoreProjectionHost(EventStoreClient eventStore, IMessageTypeMapper messageTypeMap,
		IStreamStore streamStore, params StreamStoreProjection[] projections) {
		_eventStore = eventStore;
		_messageTypeMap = messageTypeMap;
		_streamStore = streamStore;
		_projections = projections;
		_stopped = new CancellationTokenSource();
	}

	public Task StartAsync(CancellationToken cancellationToken) {
		if (_projections.Length > 0) {
			Subscribe();
		}

		return Task.CompletedTask;
	}

	public Task StopAsync(CancellationToken cancellationToken) {
		_stopped.Cancel();
		return Task.CompletedTask;
	}

	private async void Subscribe() {
		var projections = await GetProjectors();
		var checkpoint = projections.Select(x => x.Checkpoint).Min();

		await Policy.Handle<Exception>(ex => ex is not OperationCanceledException)
			.WaitAndRetryAsync(5, retryCount => TimeSpan.FromMilliseconds(retryCount * retryCount * 100))
			.ExecuteAsync(Subscribe);

		return;

		async Task Subscribe() {
			await using var subscription = _eventStore.SubscribeToAll(checkpoint,
				filterOptions: new SubscriptionFilterOptions(EventTypeFilter.ExcludeSystemEvents()));

			await foreach (var message in subscription.Messages) {
				if (message is not StreamMessage.Event(var resolvedEvent)) {
					continue;
				}

				if (!_messageTypeMap.TryMap(resolvedEvent.Event.EventType, out var type)) {
					continue;
				}

				var e = JsonSerializer.Deserialize(resolvedEvent.Event.Data.Span, type!,
					TransactoSerializerOptions.Events)!;

				await Task.WhenAll(projections
					.Where(x => x.Checkpoint < FromAll.After(resolvedEvent.OriginalPosition ?? Position.Start))
					.Select(_ => _.Projector.ProjectAsync(_streamStore,
						new Envelope(message, resolvedEvent.OriginalEvent.Position), _stopped.Token)));
			}
		}

		Task<CheckpointedProjector[]> GetProjectors() => Task.WhenAll(Array.ConvertAll(_projections,
			async projection => new CheckpointedProjector(
				new Projector<IStreamStore>(Resolve.WhenAssignableToHandlerMessageType(projection.Handlers)),
				await projection.ReadCheckpoint(_streamStore, _stopped.Token))));

	}

	private record CheckpointedProjector(Projector<IStreamStore> Projector, FromAll Checkpoint);
}

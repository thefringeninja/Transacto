using System.Text.Json;
using KurrentDB.Client;
using Polly;
using Projac;
using SqlStreamStore;
using Transacto.Framework;

namespace Transacto.Infrastructure.SqlStreamStore;

public class StreamStoreProjectionHost(
	KurrentDBClient eventStore,
	IMessageTypeMapper messageTypeMap,
	IStreamStore streamStore,
	params StreamStoreProjection[] projections)
	: IHostedService {
	private readonly CancellationTokenSource _stopped = new();

	public Task StartAsync(CancellationToken cancellationToken) {
		if (projections.Length > 0) {
			_ = Subscribe();
		}

		return Task.CompletedTask;
	}

	public Task StopAsync(CancellationToken cancellationToken) {
		_stopped.Cancel();
		return Task.CompletedTask;
	}

	private async Task Subscribe() {
		var projections1 = await GetProjectors();
		var checkpoint = projections1.Select(x => x.Checkpoint).Min();

		await Policy.Handle<Exception>(ex => ex is not OperationCanceledException)
			.WaitAndRetryAsync(5, retryCount => TimeSpan.FromMilliseconds(retryCount * retryCount * 100))
			.ExecuteAsync(Subscribe);

		return;

		async Task Subscribe() {
			await using var subscription = eventStore.SubscribeToAll(checkpoint,
				filterOptions: new SubscriptionFilterOptions(EventTypeFilter.ExcludeSystemEvents()));

			await foreach (var message in subscription.Messages.WithCancellation(_stopped.Token)) {
				if (message is not StreamMessage.Event(var resolvedEvent)) {
					continue;
				}

				if (!messageTypeMap.TryMap(resolvedEvent.Event.EventType, out var type)) {
					continue;
				}

				var e = JsonSerializer.Deserialize(resolvedEvent.Event.Data.Span, type!,
					TransactoSerializerOptions.Events)!;

				await Task.WhenAll(projections1
					.Where(x => x.Checkpoint < FromAll.After(resolvedEvent.OriginalPosition ?? Position.Start))
					.Select(_ => _.Projector.ProjectAsync(streamStore,
						new Envelope(message, resolvedEvent.OriginalEvent.Position), _stopped.Token)));
			}
		}

		Task<CheckpointedProjector[]> GetProjectors() => Task.WhenAll(Array.ConvertAll(projections,
			async projection => new CheckpointedProjector(
				new Projector<IStreamStore>(Resolve.WhenAssignableToHandlerMessageType(projection.Handlers)),
				await projection.ReadCheckpoint(streamStore, _stopped.Token))));

	}

	private record CheckpointedProjector(Projector<IStreamStore> Projector, FromAll Checkpoint);
}

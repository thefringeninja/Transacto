using System.Text.Json;
using EventStore.Client;
using Polly;

namespace Transacto.Framework.ProcessManagers;

public class ProcessManagerHost : IHostedService {
	private readonly EventStoreClient _eventStore;
	private readonly IMessageTypeMapper _messageTypeMapper;
	private readonly string _checkpointStreamName;
	private readonly CancellationTokenSource _stopped;
	private readonly ProcessManagerEventDispatcher _dispatcher;


	public ProcessManagerHost(EventStoreClient eventStore, IMessageTypeMapper messageTypeMapper,
		string checkpointStreamName, ProcessManagerEventHandlerModule eventHandlerModule) {
		_eventStore = eventStore;
		_messageTypeMapper = messageTypeMapper;
		_checkpointStreamName = checkpointStreamName;
		_stopped = new CancellationTokenSource();

		_dispatcher = new ProcessManagerEventDispatcher(eventHandlerModule);
	}

	public async Task StartAsync(CancellationToken cancellationToken) {
		await SetStreamMetadata(cancellationToken);
		Subscribe();
	}

	public Task StopAsync(CancellationToken cancellationToken) {
		_stopped.Cancel();
		return Task.CompletedTask;
	}

	private Task SetStreamMetadata(CancellationToken cancellationToken) =>
		_eventStore.SetStreamMetadataAsync(_checkpointStreamName, StreamState.NoStream,
			new StreamMetadata(maxCount: 5), options => options.ThrowOnAppendFailure = false,
			cancellationToken: cancellationToken);

	private async void Subscribe() {
		var checkpoint = await ReadCheckpoint();

		await Policy.Handle<Exception>(ex => ex is not OperationCanceledException)
			.RetryForeverAsync((_, retryCount) =>
				Task.Delay(TimeSpan.FromMilliseconds(Math.Max(retryCount * retryCount * 100, 1000))))
			.ExecuteAsync(Subscribe);
		return;

		async Task Subscribe() {
			await using var subscription = _eventStore.SubscribeToAll(checkpoint.ToEventStorePosition(),
				filterOptions: new SubscriptionFilterOptions(EventTypeFilter.ExcludeSystemEvents()));

			await foreach (var message in subscription.Messages) {
				if (message is not StreamMessage.Event (var resolvedEvent)) {
					continue;
				}

				if (!_messageTypeMapper.TryMap(resolvedEvent.Event.EventType, out var type)) {
					return;
				}

				var e = JsonSerializer.Deserialize(resolvedEvent.Event.Data.Span, type,
					TransactoSerializerOptions.Events)!;

				checkpoint = await _dispatcher.Handle(e, _stopped.Token);

				if (checkpoint == Checkpoint.None) {
					continue;
				}

				await _eventStore.AppendToStreamAsync(_checkpointStreamName, StreamState.Any, new[] {
					new EventData(Uuid.NewUuid(), "checkpoint", checkpoint.Memory,
						contentType: "application/octet-stream")
				}, cancellationToken: _stopped.Token);
			}
		}
	}

	private ValueTask<Checkpoint> ReadCheckpoint() =>
		_eventStore.ReadStreamAsync(Direction.Backwards, _checkpointStreamName,
				StreamPosition.End, 1, cancellationToken: _stopped.Token)
			.Messages
			.OfType<StreamMessage.Event>()
			.Select(e => new Checkpoint(e.ResolvedEvent.Event.Data))
			.SingleOrDefaultAsync(_stopped.Token);
}

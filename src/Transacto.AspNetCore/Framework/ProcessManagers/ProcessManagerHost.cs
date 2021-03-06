using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EventStore.Client;
using Microsoft.Extensions.Hosting;

namespace Transacto.Framework.ProcessManagers {
	public class ProcessManagerHost : IHostedService {
		private readonly EventStoreClient _eventStore;
		private readonly IMessageTypeMapper _messageTypeMapper;
		private readonly string _checkpointStreamName;
		private readonly CancellationTokenSource _stopped;
		private readonly ProcessManagerEventDispatcher _dispatcher;

		private int _subscribed;
		private StreamSubscription? _subscription;
		private CancellationTokenRegistration? _stoppedRegistration;
		private Checkpoint _checkpoint;

		public ProcessManagerHost(EventStoreClient eventStore, IMessageTypeMapper messageTypeMapper,
			string checkpointStreamName, ProcessManagerEventHandlerModule eventHandlerModule) {
			_eventStore = eventStore;
			_messageTypeMapper = messageTypeMapper;
			_checkpointStreamName = checkpointStreamName;
			_stopped = new CancellationTokenSource();

			_subscribed = 0;
			_subscription = null;
			_stoppedRegistration = null;
			_dispatcher = new ProcessManagerEventDispatcher(eventHandlerModule);
			_checkpoint = Checkpoint.None;
		}

		public async Task StartAsync(CancellationToken cancellationToken) {
			await SetStreamMetadata(cancellationToken);
			await Subscribe(cancellationToken);
		}

		public Task StopAsync(CancellationToken cancellationToken) {
			_stopped.Cancel();
			_stoppedRegistration?.Dispose();
			return Task.CompletedTask;
		}

		private async Task SetStreamMetadata(CancellationToken cancellationToken) {
			await _eventStore.SetStreamMetadataAsync(_checkpointStreamName, StreamState.NoStream,
				new StreamMetadata(maxCount: 5), options => options.ThrowOnAppendFailure = false, cancellationToken: cancellationToken);
		}

		private async Task Subscribe(CancellationToken cancellationToken) {
			if (Interlocked.CompareExchange(ref _subscribed, 1, 0) == 1) {
				return;
			}

			var registration = _stoppedRegistration;
			if (registration != null) {
				await registration.Value.DisposeAsync();
			}

			Interlocked.Exchange(ref _subscription, await Subscribe());

			_stoppedRegistration = _stopped.Token.Register(_subscription.Dispose);

			async Task<StreamSubscription> Subscribe() {
				await using var result = _eventStore.ReadStreamAsync(Direction.Backwards, _checkpointStreamName,
					StreamPosition.End, cancellationToken: cancellationToken);

				_checkpoint = await result.ReadState == ReadState.StreamNotFound
					? Checkpoint.None
					: await result.Select(e => new Checkpoint(e.Event.Data)).FirstOrDefaultAsync(cancellationToken);

				return await _eventStore.SubscribeToAllAsync(
					_checkpoint.ToEventStorePosition(), HandleAsync, subscriptionDropped: (_, reason, _) => {
						if (reason == SubscriptionDroppedReason.Disposed) {
							return;
						}

						//Log.Error(ex, "Subscription dropped: {reason}", reason);
					},
					filterOptions: new SubscriptionFilterOptions(EventTypeFilter.ExcludeSystemEvents()),
					userCredentials: new UserCredentials("admin", "changeit"),
					cancellationToken: _stopped.Token);
			}

			async Task HandleAsync(StreamSubscription s, ResolvedEvent e, CancellationToken ct) {
				if (!_messageTypeMapper.TryMap(e.Event.EventType, out var type)) {
					return;
				}

				var message = JsonSerializer.Deserialize(e.Event.Data.Span, type, TransactoSerializerOptions.Events)!;

				_checkpoint = await _dispatcher.Handle(message, ct);

				if (_checkpoint == Checkpoint.None) {
					return;
				}

				await _eventStore.AppendToStreamAsync(_checkpointStreamName, StreamState.Any, new[] {
					new EventData(Uuid.NewUuid(), "checkpoint", _checkpoint.Memory,
						contentType: "application/octet-stream")
				}, cancellationToken: ct);
			}
		}
	}
}

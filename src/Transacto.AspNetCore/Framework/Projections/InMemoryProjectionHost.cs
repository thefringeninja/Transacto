using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EventStore.Client;
using Microsoft.Extensions.Hosting;
using Projac;
using Serilog;

namespace Transacto.Framework.Projections {
	public class InMemoryProjectionHost : IHostedService {
		private readonly EventStoreClient _eventStore;
		private readonly IMessageTypeMapper _messageTypeMapper;
		private readonly InMemorySession _target;
		private readonly CancellationTokenSource _stopped;

		private int _subscribed;
		private StreamSubscription? _subscription;
		private CancellationTokenRegistration? _stoppedRegistration;
		private readonly Projector<InMemorySession> _projector;

		public InMemoryProjectionHost(EventStoreClient eventStore, IMessageTypeMapper messageTypeMapper,
			InMemorySession target, params ProjectionHandler<InMemorySession>[][] projections) {
			_eventStore = eventStore;
			_messageTypeMapper = messageTypeMapper;
			_target = target;
			_stopped = new CancellationTokenSource();

			_subscribed = 0;
			_subscription = null;
			_stoppedRegistration = null;

			_projector = new Projector<InMemorySession>(
				Resolve.WhenEqualToHandlerMessageType(projections.SelectMany(_ => _).ToArray()));
		}

		public Task StartAsync(CancellationToken cancellationToken) => Subscribe(cancellationToken);

		private async Task Subscribe(CancellationToken cancellationToken) {
			if (Interlocked.CompareExchange(ref _subscribed, 1, 0) == 1) {
				return;
			}

			var registration = _stoppedRegistration;
			if (registration != null) {
				await registration.Value.DisposeAsync();
			}

			Interlocked.Exchange(ref _subscription, await _eventStore.SubscribeToAllAsync(
				Position.Start,
				ProjectAsync,
				subscriptionDropped: (_, reason, ex) => {
					if (reason == SubscriptionDroppedReason.Disposed) {
						return;
					}

					Log.Error(ex, "Subscription dropped: {reason}", reason);
				},
				filterOptions: new SubscriptionFilterOptions(EventTypeFilter.ExcludeSystemEvents()),
				userCredentials: new UserCredentials("admin", "changeit"),
				cancellationToken: _stopped.Token));

			_stoppedRegistration = _stopped.Token.Register(_subscription.Dispose);

			Task ProjectAsync(StreamSubscription s, ResolvedEvent e, CancellationToken ct) =>
				_messageTypeMapper.TryMap(e.Event.EventType, out var type)
					?_projector.ProjectAsync(_target, Envelope.Create(JsonSerializer.Deserialize(
						e.Event.Data.Span, type, TransactoSerializerOptions.Events)!, e.OriginalEvent.Position), ct)
					: Task.CompletedTask;
		}

		public Task StopAsync(CancellationToken cancellationToken) {
			_stopped.Cancel();
			_stoppedRegistration?.Dispose();
			return Task.CompletedTask;
		}
	}
}

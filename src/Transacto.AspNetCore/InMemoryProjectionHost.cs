using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EventStore.Client;
using Microsoft.Extensions.Hosting;
using Projac;
using Serilog;
using Transacto.Framework;
using Transacto.Infrastructure;

#nullable enable
namespace Transacto {
	public class InMemoryProjectionHost : IHostedService {
		private readonly EventStoreClient _eventStore;
		private readonly IMessageTypeMapper _messageTypeMapper;
		private readonly InMemoryReadModel _target;
		private readonly CancellationTokenSource _stopped;

		private int _retryCount;
		private int _subscribed;
		private StreamSubscription? _subscription;
		private CancellationTokenRegistration? _stoppedRegistration;
		private readonly Projector<InMemoryReadModel> _projector;

		public InMemoryProjectionHost(EventStoreClient eventStore, IMessageTypeMapper messageTypeMapper,
			InMemoryReadModel target, params ProjectionHandler<InMemoryReadModel>[][] projections) {
			_eventStore = eventStore;
			_messageTypeMapper = messageTypeMapper;
			_target = target;
			_stopped = new CancellationTokenSource();

			_retryCount = 0;
			_subscribed = 0;
			_subscription = null;
			_stoppedRegistration = null;

			_projector =
				new Projector<InMemoryReadModel>(
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

			Interlocked.Exchange(ref _subscription, await _eventStore.SubscribeToAllAsync(ProjectAsync,
				subscriptionDropped: (_, reason, ex) => {
					if (reason == SubscriptionDroppedReason.Disposed) {
						return;
					}

					if (Interlocked.Increment(ref _retryCount) == 5) {
						Log.Error(ex, "Subscription dropped: {reason}", reason);
						return;
					}

					Log.Warning(ex, "Subscription dropped: {reason}; resubscribing...", reason);
					Interlocked.Exchange(ref _subscribed, 0);
					Task.Run(() => Subscribe(cancellationToken), cancellationToken);
				},
				filterOptions: new SubscriptionFilterOptions(EventTypeFilter.ExcludeSystemEvents()),
				userCredentials: new UserCredentials("admin", "changeit"),
				cancellationToken: _stopped.Token));

			_stoppedRegistration = _stopped.Token.Register(_subscription.Dispose);

			Task ProjectAsync(StreamSubscription s, ResolvedEvent e, CancellationToken ct) {
				var type = _messageTypeMapper.Map(e.Event.EventType);
				if (type == null)
					return Task.CompletedTask;
				var message = JsonSerializer.Deserialize(
					e.Event.Data.Span, type, TransactoSerializerOptions.Events);
				return _projector.ProjectAsync(_target, message, ct);
			}
		}

		public Task StopAsync(CancellationToken cancellationToken) {
			_stopped.Cancel();
			_stoppedRegistration?.Dispose();
			return Task.CompletedTask;
		}
	}
}

using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EventStore.Client;
using Microsoft.Extensions.Hosting;
using Transacto.Domain;
using Transacto.Framework;
using Transacto.Infrastructure;
using Transacto.Messages;
using Transacto.Modules;

namespace Transacto.Application {
	public class AccountClosingProcess {
		private readonly EventStoreClient _eventStore;
		private readonly IMessageTypeMapper _messageTypeMapper;
		private readonly CancellationTokenSource _stopped;
		private readonly CommandDispatcher _dispatcher;

		private int _subscribed;
		private StreamSubscription? _subscription;
		private CancellationTokenRegistration? _stoppedRegistration;

		public AccountClosingProcess(EventStoreClient eventStore, IMessageTypeMapper messageTypeMapper) {
			_eventStore = eventStore;
			_messageTypeMapper = messageTypeMapper;
			_stopped = new CancellationTokenSource();

			_subscribed = 0;
			_subscription = null;
			_stoppedRegistration = null;
			_dispatcher = new CommandDispatcher(new[] {
				new GeneralLedgerModule(eventStore, messageTypeMapper, null!)
			});
		}

		public Task StartAsync(CancellationToken cancellationToken) => Subscribe(cancellationToken);

		public Task StopAsync(CancellationToken cancellationToken) {
			_stopped.Cancel();
			_stoppedRegistration?.Dispose();
			return Task.CompletedTask;
		}

		private async Task Subscribe(CancellationToken cancellationToken) {
			if (Interlocked.CompareExchange(ref _subscribed, 1, 0) == 1) {
				return;
			}

			var registration = _stoppedRegistration;
			if (registration != null) {
				await registration.Value.DisposeAsync();
			}

			Interlocked.Exchange(ref _subscription, await _eventStore.SubscribeToStreamAsync(
				GeneralLedger.Identifier, HandleAsync, subscriptionDropped: (_, reason, ex) => {
					if (reason == SubscriptionDroppedReason.Disposed) {
						return;
					}

					//Log.Error(ex, "Subscription dropped: {reason}", reason);
				},
				userCredentials: new UserCredentials("admin", "changeit"),
				cancellationToken: _stopped.Token));

			_stoppedRegistration = _stopped.Token.Register(_subscription.Dispose);

			async Task HandleAsync(StreamSubscription s, ResolvedEvent e, CancellationToken ct) {
				var type = _messageTypeMapper.Map(e.Event.EventType);
				if (type == null) {
					return;
				}

				var message = JsonSerializer.Deserialize(
					e.Event.Data.Span, type, TransactoSerializerOptions.Events);
				if (message is AccountingPeriodClosing) {
					await _dispatcher.Handle(e, ct);
				}
			}
		}
	}
}

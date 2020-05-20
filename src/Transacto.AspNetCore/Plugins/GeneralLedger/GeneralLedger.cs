using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EventStore.Client;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Transacto.Domain;
using Transacto.Framework;
using Transacto.Infrastructure;
using Transacto.Messages;
using Transacto.Modules;

namespace Transacto.Plugins.GeneralLedger {
	internal class GeneralLedger : IPlugin {
		public string Name { get; } = nameof(GeneralLedger);

		public void Configure(IEndpointRouteBuilder builder) => builder
			.MapBusinessTransaction<JournalEntry>("/entries")
			.MapCommands(string.Empty,
				typeof(OpenGeneralLedger),
				typeof(BeginClosingAccountingPeriod));

		public void ConfigureServices(IServiceCollection services) => services
			.AddInMemoryProjection(new InMemoryProjectionBuilder()
				.When<GeneralLedgerEntryPosted>((readModel, e) =>
					readModel.AddOrUpdate(e.Message.Period,
						() => new List<Guid> {e.Message.GeneralLedgerEntryId},
						l => l.Add(e.Message.GeneralLedgerEntryId)))
				.When<AccountingPeriodClosed>((readModel, e) => {
					if (!readModel.TryRemove<List<Guid>>(e.Message.Period, out var value)) {
						return;
					}

					var notClosed = value.Except(e.Message.GeneralLedgerEntryIds)
						.Except(new[] {e.Message.ClosingGeneralLedgerEntryId})
						.ToList();

					if (notClosed.Count == 0) {
						return;
					}

					readModel.AddOrUpdate(nameof(notClosed), () => notClosed, x => x.AddRange(notClosed));
				})
				.Build());

		public IEnumerable<Type> MessageTypes => Enumerable.Empty<Type>();

		private class AccountClosingProcess : IHostedService {
			private EventStoreClient _eventStore;
			private IMessageTypeMapper _messageTypeMapper;
			private CancellationTokenSource _stopped;
			private int _subscribed;
			private StreamSubscription? _subscription;
			private CancellationTokenRegistration? _stoppedRegistration;
			private readonly CommandDispatcher _dispatcher;


			public AccountClosingProcess(EventStoreClient eventStore, IMessageTypeMapper messageTypeMapper) {
				_eventStore = eventStore;
				_messageTypeMapper = messageTypeMapper;
				_stopped = new CancellationTokenSource();

				_subscribed = 0;
				_subscription = null;
				_stoppedRegistration = null;
				_dispatcher = new CommandDispatcher(new[] {
					new GeneralLedgerModule(eventStore, messageTypeMapper, TransactoSerializerOptions.Events)
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
					Domain.GeneralLedger.Identifier, HandleAsync, subscriptionDropped: (_, reason, ex) => {
						if (reason == SubscriptionDroppedReason.Disposed) {
							return;
						}

						Log.Error(ex, "Subscription dropped: {reason}", reason);
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
}

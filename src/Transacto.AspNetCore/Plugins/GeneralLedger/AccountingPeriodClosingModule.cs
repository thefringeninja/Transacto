using EventStore.Client;
using Transacto.Application;
using Transacto.Domain;
using Transacto.Framework;
using Transacto.Framework.ProcessManagers;
using Transacto.Infrastructure;
using Transacto.Messages;

namespace Transacto.Plugins.GeneralLedger {
	public class AccountingPeriodClosingModule : ProcessManagerEventHandlerModule {
		public AccountingPeriodClosingModule(EventStoreClient eventStore, IMessageTypeMapper messageTypeMapper,
			AccountIsDeactivated accountIsDeactivated) {
			var handlers =
				new AccountingPeriodClosingHandlers(
					new GeneralLedgerEventStoreRepository(eventStore, messageTypeMapper),
					new GeneralLedgerEntryEventStoreRepository(eventStore, messageTypeMapper),
					accountIsDeactivated);
			Build<AccountingPeriodClosing>()
				.Log()
				.UnitOfWork(eventStore, messageTypeMapper)
				.Handle(async (command, ct) => {
					await handlers.Handle(command, ct);
					return Position.Start;
				});
		}
	}
}

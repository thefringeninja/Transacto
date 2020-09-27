using EventStore.Client;
using Transacto.Application;
using Transacto.Domain;
using Transacto.Framework;
using Transacto.Framework.CommandHandling;
using Transacto.Infrastructure;
using Transacto.Messages;

namespace Transacto.Plugins.GeneralLedger {
	public class AccountingPeriodClosingModule : CommandHandlerModule {
		public AccountingPeriodClosingModule(EventStoreClient eventStore, IMessageTypeMapper messageTypeMapper,
			AccountIsDeactivated accountIsDeactivated) {
			var handlers =
				new AccountingPeriodClosingHandlers(
					new GeneralLedgerEventStoreRepository(eventStore, messageTypeMapper),
					new GeneralLedgerEntryEventStoreRepository(eventStore, messageTypeMapper),
					accountIsDeactivated);
			Build<AccountingPeriodClosing>()
				.Log()
				.UnitOfWork(eventStore, messageTypeMapper, TransactoSerializerOptions.Events)
				.Handle(async (command, ct) => {
					await handlers.Handle(command, ct);
					return Position.Start;
				});
		}
	}
}

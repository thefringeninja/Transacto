using KurrentDB.Client;
using Transacto.Application;
using Transacto.Framework;
using Transacto.Framework.ProcessManagers;
using Transacto.Infrastructure.EventStore;
using Transacto.Messages;

namespace Transacto.Plugins.GeneralLedger;

public class AccountingPeriodClosingModule : ProcessManagerEventHandlerModule {
	public AccountingPeriodClosingModule(KurrentDBClient eventStore, IMessageTypeMapper messageTypeMapper,
		ILogger<AccountingPeriodClosingModule> logger) {
		var handlers = new AccountingPeriodClosingHandlers(
			new GeneralLedgerEventStoreRepository(eventStore, messageTypeMapper),
			new GeneralLedgerEntryEventStoreRepository(eventStore, messageTypeMapper),
			new ChartOfAccountsEventStoreRepository(eventStore, messageTypeMapper));
		Build<AccountingPeriodClosing>()
			.Log(logger)
			.UnitOfWork(eventStore, messageTypeMapper)
			.Handle(async (command, ct) => {
				await handlers.Handle(command, ct);
				return Checkpoint.None;
			});
	}
}

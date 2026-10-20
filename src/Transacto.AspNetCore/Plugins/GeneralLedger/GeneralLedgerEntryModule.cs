using KurrentDB.Client;
using Transacto.Application;
using Transacto.Framework;
using Transacto.Framework.CommandHandling;
using Transacto.Infrastructure.EventStore;
using Transacto.Messages;

namespace Transacto.Plugins.GeneralLedger;

public class GeneralLedgerEntryModule : CommandHandlerModule {
	public GeneralLedgerEntryModule(KurrentDBClient eventStore, IMessageTypeMapper messageTypeMapper,
		ILogger<GeneralLedgerEntryModule> logger) {
		var handlers = new GeneralLedgerEntryHandlers(
			new GeneralLedgerEventStoreRepository(eventStore, messageTypeMapper),
			new GeneralLedgerEntryEventStoreRepository(eventStore, messageTypeMapper),
			new ChartOfAccountsEventStoreRepository(eventStore, messageTypeMapper));
		Build<PostGeneralLedgerEntry>()
			.Log(logger)
			.UnitOfWork(eventStore, messageTypeMapper)
			.Handle(async (command, ct) => {
				await handlers.Handle(command, ct);

				return Checkpoint.None;
			});
	}
}

using EventStore.Client;
using Transacto.Application;
using Transacto.Framework;
using Transacto.Framework.CommandHandling;
using Transacto.Infrastructure.EventStore;
using Transacto.Messages;

namespace Transacto.Plugins.GeneralLedger;

internal class GeneralLedgerModule : CommandHandlerModule {
	public GeneralLedgerModule(EventStoreClient eventStore, IMessageTypeMapper messageTypeMapper,
		ILogger<GeneralLedgerModule> logger) {
		var handlers = new GeneralLedgerHandlers(
			new GeneralLedgerEventStoreRepository(eventStore, messageTypeMapper),
			new ChartOfAccountsEventStoreRepository(eventStore, messageTypeMapper));
		Build<OpenGeneralLedger>()
			.Log(logger)
			.UnitOfWork(eventStore, messageTypeMapper)
			.Handle(async (command, ct) => {
				await handlers.Handle(command, ct);
				return Checkpoint.None;
			});
		Build<BeginClosingAccountingPeriod>()
			.Log(logger)
			.UnitOfWork(eventStore, messageTypeMapper)
			.Handle(async (command, ct) => {
				await handlers.Handle(command, ct);
				return Checkpoint.None;
			});
	}
}

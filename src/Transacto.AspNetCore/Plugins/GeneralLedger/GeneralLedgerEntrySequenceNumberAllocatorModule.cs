using KurrentDB.Client;
using Transacto.Application;
using Transacto.Domain;
using Transacto.Framework;
using Transacto.Framework.ProcessManagers;
using Transacto.Infrastructure.EventStore;
using Transacto.Messages;

namespace Transacto.Plugins.GeneralLedger;

public class GeneralLedgerEntrySequenceNumberAllocatorModule : ProcessManagerEventHandlerModule {
	public GeneralLedgerEntrySequenceNumberAllocatorModule(KurrentDBClient eventStore,
		IMessageTypeMapper messageTypeMapper,
		GetSequenceNumbers getSequenceNumbers,
		ILogger<GeneralLedgerEntrySequenceNumberAllocatorModule> logger) {
		var handlers = new GeneralLedgerEntrySequenceNumberAllocatorHandlers(
			new GeneralLedgerEntryEventStoreRepository(eventStore, messageTypeMapper),
			getSequenceNumbers);
		Build<GeneralLedgerEntryPosted>()
			.Log(logger)
			.UnitOfWork(eventStore, messageTypeMapper)
			.Handle(async (e, ct) => {
				await handlers.Handle(e, ct);
				return Checkpoint.None;
			});
		Build<GeneralLedgerEntryNumberAllocated>()
			.Log(logger)
			.UnitOfWork(eventStore, messageTypeMapper)
			.Handle(async (e, ct) => {
				await handlers.Handle(e, ct);
				return Checkpoint.None;
			});
	}
}

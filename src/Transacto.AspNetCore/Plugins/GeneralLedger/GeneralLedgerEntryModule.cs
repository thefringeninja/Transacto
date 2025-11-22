using KurrentDB.Client;
using Transacto.Application;
using Transacto.Domain;
using Transacto.Framework;
using Transacto.Framework.CommandHandling;
using Transacto.Infrastructure.EventStore;
using Transacto.Messages;

namespace Transacto.Plugins.GeneralLedger;

internal class GeneralLedgerEntryModule : CommandHandlerModule {
	public GeneralLedgerEntryModule(KurrentDBClient eventStore, IMessageTypeMapper messageTypeMapper,
		GetPrefix getPrefix, AccountIsDeactivated accountIsDeactivated, ILogger<GeneralLedgerEntryModule> logger) {
		var handlers = new GeneralLedgerEntryHandlers(
			new GeneralLedgerEventStoreRepository(eventStore, messageTypeMapper),
			new GeneralLedgerEntryEventStoreRepository(eventStore, messageTypeMapper), getPrefix,
			accountIsDeactivated);
		Build<PostGeneralLedgerEntry>()
			.Log(logger)
			.UnitOfWork(eventStore, messageTypeMapper)
			.Handle(async (command, ct) => {
				await handlers.Handle(command, ct);

				return Checkpoint.None;
			});
	}
}

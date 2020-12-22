using EventStore.Client;
using Transacto.Application;
using Transacto.Domain;
using Transacto.Framework;
using Transacto.Framework.CommandHandling;
using Transacto.Infrastructure;
using Transacto.Messages;

namespace Transacto.Plugins.GeneralLedger {
	internal class GeneralLedgerEntryModule : CommandHandlerModule {
		public GeneralLedgerEntryModule(EventStoreClient eventStore, IMessageTypeMapper messageTypeMapper,
			AccountIsDeactivated accountIsDeactivated) {
			var handlers = new GeneralLedgerEntryHandlers(
				new GeneralLedgerEventStoreRepository(eventStore, messageTypeMapper),
				new GeneralLedgerEntryEventStoreRepository(eventStore, messageTypeMapper),
				accountIsDeactivated);
			Build<PostGeneralLedgerEntry>()
				.Log()
				.UnitOfWork(eventStore, messageTypeMapper)
				.Handle(async (command, ct) => {
					await handlers.Handle(command, ct);

					return Checkpoint.None;
				});
		}
	}
}

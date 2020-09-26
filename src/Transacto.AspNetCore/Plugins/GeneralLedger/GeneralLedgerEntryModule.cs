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
			Build<PostGeneralLedgerEntry>()
				.Log()
				.UnitOfWork(eventStore, messageTypeMapper, TransactoSerializerOptions.Events)
				.Handle(async (_, ct) => {
					var (unitOfWork, command) = _;
					var handlers = new GeneralLedgerEntryHandlers(
						new GeneralLedgerEventStoreRepository(eventStore, messageTypeMapper, unitOfWork),
						new GeneralLedgerEntryEventStoreRepository(eventStore, messageTypeMapper, unitOfWork),
						accountIsDeactivated);

					await handlers.Handle(command, ct);

					return Position.Start;
				});
		}
	}
}

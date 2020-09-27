using EventStore.Client;
using Transacto.Application;
using Transacto.Framework;
using Transacto.Framework.CommandHandling;
using Transacto.Infrastructure;
using Transacto.Messages;

namespace Transacto.Plugins.GeneralLedger {
	internal class GeneralLedgerModule : CommandHandlerModule {
		public GeneralLedgerModule(EventStoreClient eventStore, IMessageTypeMapper messageTypeMapper) {
			var handlers =
				new GeneralLedgerHandlers(
					new GeneralLedgerEventStoreRepository(eventStore, messageTypeMapper));
			Build<OpenGeneralLedger>()
				.Log()
				.UnitOfWork(eventStore, messageTypeMapper, TransactoSerializerOptions.Events)
				.Handle(async (command, ct) => {
					await handlers.Handle(command, ct);
					return Position.Start;
				});
			Build<BeginClosingAccountingPeriod>()
				.Log()
				.UnitOfWork(eventStore, messageTypeMapper, TransactoSerializerOptions.Events)
				.Handle(async (command, ct) => {
					await handlers.Handle(command, ct);
					return Position.Start;
				});
		}
	}
}

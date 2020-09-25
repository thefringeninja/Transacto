using EventStore.Client;
using Transacto.Application;
using Transacto.Domain;
using Transacto.Framework;
using Transacto.Framework.CommandHandling;
using Transacto.Infrastructure;
using Transacto.Messages;

namespace Transacto.Modules {
	public class GeneralLedgerModule : CommandHandlerModule {
		public GeneralLedgerModule(EventStoreClient eventStore, IMessageTypeMapper messageTypeMapper,
			AccountIsDeactivated accountIsDeactivated) {
			Build<OpenGeneralLedger>()
				.Log()
				.UnitOfWork(eventStore, messageTypeMapper, TransactoSerializerOptions.Events)
				.Handle(async (_, ct) => {
					var (unitOfWork, command) = _;
					var handlers =
						new GeneralLedgerHandlers(
							new GeneralLedgerEventStoreRepository(eventStore, messageTypeMapper, unitOfWork),
							new GeneralLedgerEntryEventStoreRepository(eventStore, messageTypeMapper, unitOfWork),
							accountIsDeactivated);

					await handlers.Handle(command, ct);
					return Position.Start;
				});
			Build<BeginClosingAccountingPeriod>()
				.Log()
				.UnitOfWork(eventStore, messageTypeMapper, TransactoSerializerOptions.Events)
				.Handle(async (_, ct) => {
					var (unitOfWork, command) = _;
					var handlers =
						new GeneralLedgerHandlers(
							new GeneralLedgerEventStoreRepository(eventStore, messageTypeMapper, unitOfWork),
							new GeneralLedgerEntryEventStoreRepository(eventStore, messageTypeMapper, unitOfWork),
							accountIsDeactivated);

					await handlers.Handle(command, ct);
					return Position.Start;
				});
			Build<AccountingPeriodClosing>()
				.Log()
				.UnitOfWork(eventStore, messageTypeMapper, TransactoSerializerOptions.Events)
				.Handle(async (_, ct) => {
					var (unitOfWork, command) = _;
					var handlers =
						new GeneralLedgerHandlers(
							new GeneralLedgerEventStoreRepository(eventStore, messageTypeMapper, unitOfWork),
							new GeneralLedgerEntryEventStoreRepository(eventStore, messageTypeMapper, unitOfWork),
							accountIsDeactivated);

					await handlers.Handle(command, ct);
					return Position.Start;
				});
		}
	}
}

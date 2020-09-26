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
			Build<AccountingPeriodClosing>()
				.Log()
				.UnitOfWork(eventStore, messageTypeMapper, TransactoSerializerOptions.Events)
				.Handle(async (_, ct) => {
					var (unitOfWork, command) = _;
					var handlers =
						new AccountingPeriodClosingHandlers(
							new GeneralLedgerEventStoreRepository(eventStore, messageTypeMapper, unitOfWork),
							new GeneralLedgerEntryEventStoreRepository(eventStore, messageTypeMapper, unitOfWork),
							accountIsDeactivated);

					await handlers.Handle(command, ct);
					return Position.Start;
				});
		}
	}
}

using EventStore.Client;
using Transacto.Application;
using Transacto.Framework;
using Transacto.Framework.CommandHandling;
using Transacto.Infrastructure;
using Transacto.Messages;

namespace Transacto.Plugins.ChartOfAccounts {
	internal class ChartOfAccountsModule : CommandHandlerModule {
		public ChartOfAccountsModule(EventStoreClient eventStore, IMessageTypeMapper messageTypeMapper) {
			var handlers =
				new ChartOfAccountsHandlers(new ChartOfAccountsEventStoreRepository(eventStore, messageTypeMapper));
			Build<DefineAccount>()
				.Log()
				.UnitOfWork(eventStore, messageTypeMapper)
				.Handle(async (command, ct) => {
					await handlers.Handle(command, ct);

					return Position.Start;
				});

			Build<DeactivateAccount>()
				.Log()
				.UnitOfWork(eventStore, messageTypeMapper)
				.Handle(async (command, ct) => {
					await handlers.Handle(command, ct);

					return Position.Start;
				});

			Build<ReactivateAccount>()
				.Log()
				.UnitOfWork(eventStore, messageTypeMapper)
				.Handle(async (command, ct) => {
					await handlers.Handle(command, ct);

					return Position.Start;
				});

			Build<RenameAccount>()
				.Log()
				.UnitOfWork(eventStore, messageTypeMapper)
				.Handle(async (command, ct) => {
					await handlers.Handle(command, ct);

					return Position.Start;
				});
		}
	}
}

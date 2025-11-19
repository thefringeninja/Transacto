using EventStore.Client;
using Transacto.Application;
using Transacto.Framework;
using Transacto.Framework.CommandHandling;
using Transacto.Infrastructure.EventStore;
using Transacto.Messages;

namespace Transacto.Plugins.ChartOfAccounts;

internal class ChartOfAccountsModule : CommandHandlerModule {
	public ChartOfAccountsModule(EventStoreClient eventStore, IMessageTypeMapper messageTypeMapper,
		ILogger<ChartOfAccountsModule> logger) {
		var handlers =
			new ChartOfAccountsHandlers(new ChartOfAccountsEventStoreRepository(eventStore, messageTypeMapper));
		Build<DefineAccount>()
			.Log(logger)
			.UnitOfWork(eventStore, messageTypeMapper)
			.Handle(async (command, ct) => {
				await handlers.Handle(command, ct);

				return Checkpoint.None;
			});

		Build<DeactivateAccount>()
			.Log(logger)
			.UnitOfWork(eventStore, messageTypeMapper)
			.Handle(async (command, ct) => {
				await handlers.Handle(command, ct);

				return Checkpoint.None;
			});

		Build<ReactivateAccount>()
			.Log(logger)
			.UnitOfWork(eventStore, messageTypeMapper)
			.Handle(async (command, ct) => {
				await handlers.Handle(command, ct);

				return Checkpoint.None;
			});

		Build<RenameAccount>()
			.Log(logger)
			.UnitOfWork(eventStore, messageTypeMapper)
			.Handle(async (command, ct) => {
				await handlers.Handle(command, ct);

				return Checkpoint.None;
			});
	}
}

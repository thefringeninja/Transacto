using EventStore.Client;
using Transacto.Application;
using Transacto.Framework;
using Transacto.Framework.CommandHandling;
using Transacto.Infrastructure;
using Transacto.Messages;

namespace Transacto.Modules {
	public class ChartOfAccountsModule : CommandHandlerModule {
		public ChartOfAccountsModule(EventStoreClient eventStore, IMessageTypeMapper messageTypeMapper) {
			Build<DefineAccount>()
				.Log()
				.UnitOfWork(eventStore, messageTypeMapper, TransactoSerializerOptions.Events)
				.Handle(async (_, ct) => {
					var (unitOfWork, command) = _;
					var handlers = new ChartOfAccountsHandlers(
						new ChartOfAccountsEventStoreRepository(eventStore, messageTypeMapper, unitOfWork));

					await handlers.Handle(command, ct);

					return Position.Start;
				});

			Build<DeactivateAccount>()
				.Log()
				.UnitOfWork(eventStore, messageTypeMapper, TransactoSerializerOptions.Events)
				.Handle(async (_, ct) => {
					var (unitOfWork, command) = _;
					var handlers = new ChartOfAccountsHandlers(
						new ChartOfAccountsEventStoreRepository(eventStore, messageTypeMapper, unitOfWork));

					await handlers.Handle(command, ct);

					return Position.Start;
				});

			Build<ReactivateAccount>()
				.Log()
				.UnitOfWork(eventStore, messageTypeMapper, TransactoSerializerOptions.Events)
				.Handle(async (_, ct) => {
					var (unitOfWork, command) = _;
					var handlers = new ChartOfAccountsHandlers(
						new ChartOfAccountsEventStoreRepository(eventStore, messageTypeMapper, unitOfWork));

					await handlers.Handle(command, ct);

					return Position.Start;
				});

			Build<RenameAccount>()
				.Log()
				.UnitOfWork(eventStore, messageTypeMapper, TransactoSerializerOptions.Events)
				.Handle(async (_, ct) => {
					var (unitOfWork, command) = _;
					var handlers = new ChartOfAccountsHandlers(
						new ChartOfAccountsEventStoreRepository(eventStore, messageTypeMapper, unitOfWork));

					await handlers.Handle(command, ct);

					return Position.Start;
				});
		}
	}
}

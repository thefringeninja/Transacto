using System.Text.Json;
using EventStore.Client;
using Transacto.Application;
using Transacto.Framework;
using Transacto.Infrastructure;
using Transacto.Messages;
using JsonSerializerOptions = System.Text.Json.JsonSerializerOptions;

namespace Transacto.Modules {
	public class ChartOfAccountsModule : CommandHandlerModule {
		public ChartOfAccountsModule(EventStoreClient eventStore, IMessageTypeMapper messageTypeMapper,
			JsonSerializerOptions serializerOptions) {
			Build<DefineAccount>()
				.Log()
				.UnitOfWork(eventStore, messageTypeMapper, serializerOptions)
				.Handle((_, ct) => {
					var (unitOfWork, command) = _;
					var handlers = new ChartOfAccountsHandlers(
						new ChartOfAccountsEventStoreRepository(eventStore, messageTypeMapper, unitOfWork));

					return handlers.Handle(command, ct);
				});

			Build<DeactivateAccount>()
				.Log()
				.UnitOfWork(eventStore, messageTypeMapper, serializerOptions)
				.Handle((_, ct) => {
					var (unitOfWork, command) = _;
					var handlers = new ChartOfAccountsHandlers(
						new ChartOfAccountsEventStoreRepository(eventStore, messageTypeMapper, unitOfWork));

					return handlers.Handle(command, ct);
				});

			Build<ReactivateAccount>()
				.Log()
				.UnitOfWork(eventStore, messageTypeMapper, serializerOptions)
				.Handle((_, ct) => {
					var (unitOfWork, command) = _;
					var handlers = new ChartOfAccountsHandlers(
						new ChartOfAccountsEventStoreRepository(eventStore, messageTypeMapper, unitOfWork));

					return handlers.Handle(command, ct);
				});

			Build<RenameAccount>()
				.Log()
				.UnitOfWork(eventStore, messageTypeMapper, serializerOptions)
				.Handle((_, ct) => {
					var (unitOfWork, command) = _;
					var handlers = new ChartOfAccountsHandlers(
						new ChartOfAccountsEventStoreRepository(eventStore, messageTypeMapper, unitOfWork));

					return handlers.Handle(command, ct);
				});
		}
	}
}

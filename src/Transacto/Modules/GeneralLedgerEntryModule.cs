using EventStore.Client;
using Transacto.Application;
using Transacto.Framework;
using Transacto.Framework.CommandHandling;
using Transacto.Infrastructure;
using Transacto.Messages;
using JsonSerializerOptions = System.Text.Json.JsonSerializerOptions;

namespace Transacto.Modules {
	public class GeneralLedgerEntryModule : CommandHandlerModule {
		public GeneralLedgerEntryModule(EventStoreClient eventStore, IMessageTypeMapper messageTypeMapper,
			JsonSerializerOptions eventSerializerOptions) {
			Build<PostGeneralLedgerEntry>()
				.Log()
				.UnitOfWork(eventStore, messageTypeMapper, eventSerializerOptions)
				.Handle(async (_, ct) => {
					var (unitOfWork, command) = _;
					var handlers = new GeneralLedgerEntryHandlers(
						new GeneralLedgerEventStoreRepository(eventStore, messageTypeMapper, unitOfWork),
						new GeneralLedgerEntryEventStoreRepository(eventStore, messageTypeMapper, unitOfWork),
						new ChartOfAccountsEventStoreRepository(eventStore, messageTypeMapper, unitOfWork));

					await handlers.Handle(command, ct);

					return Position.Start;
				});
		}
	}
}

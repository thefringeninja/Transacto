using System.Text.Json;
using EventStore.Client;
using Transacto.Framework;
using Transacto.Framework.CommandHandling;

namespace SomeCompany.Inventory {
	public class InventoryItemModule : CommandHandlerModule {
		public InventoryItemModule(EventStoreClient eventStore,
			IMessageTypeMapper messageTypeMapper, JsonSerializerOptions serializerOptions) {
			Build<DefineInventoryItem>()
				.Log()
				.UnitOfWork(eventStore, messageTypeMapper, serializerOptions)
				.Handle(async (_, ct) => {
					var (unitOfWork, command) = _;
					var handlers = new InventoryItemHandlers(
						new InventoryItemRepository(eventStore, messageTypeMapper, unitOfWork));

					await handlers.Handle(command, ct);

					return Position.Start;
				});
		}
	}
}

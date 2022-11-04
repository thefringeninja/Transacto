using EventStore.Client;
using Transacto.Framework;
using Transacto.Framework.CommandHandling;

namespace SomeCompany.Inventory {
	public class InventoryItemModule : CommandHandlerModule {
		public InventoryItemModule(EventStoreClient eventStore, IMessageTypeMapper messageTypeMapper) {
			var handlers = new InventoryItemHandlers(new InventoryItemRepository(eventStore, messageTypeMapper));
			Build<DefineInventoryItem>()
				.Log()
				.UnitOfWork(eventStore, messageTypeMapper)
				.Handle(async (command, ct) => {
					await handlers.Handle(command, ct);

					return Checkpoint.None;
				});
		}
	}
}

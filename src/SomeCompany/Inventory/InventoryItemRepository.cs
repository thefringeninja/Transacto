using EventStore.Client;
using Transacto.Framework;
using Transacto.Infrastructure;

namespace SomeCompany.Inventory {
	public class InventoryItemRepository {
		private readonly EventStoreRepository<InventoryItem, InventoryItemIdentifier> _inner;

		public InventoryItemRepository(EventStoreClient eventStore,
			IMessageTypeMapper messageTypeMapper, UnitOfWork unitOfWork) {
			_inner = new EventStoreRepository<InventoryItem, InventoryItemIdentifier>(eventStore, unitOfWork,
				InventoryItem.Factory, item => item.Identifier, id => id.ToGuid().ToString("n"), messageTypeMapper,
				TransactoSerializerOptions.EventSerializerOptions);
		}

		public void Add(InventoryItem inventoryItem) => _inner.Add(inventoryItem);
	}
}

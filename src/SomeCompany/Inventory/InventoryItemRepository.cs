using EventStore.Client;
using Transacto.Framework;
using Transacto.Infrastructure;

namespace SomeCompany.Inventory {
	public class InventoryItemRepository {
		private readonly EventStoreRepository<InventoryItem> _inner;

		public InventoryItemRepository(EventStoreClient eventStore,
			IMessageTypeMapper messageTypeMapper, UnitOfWork unitOfWork) {
			_inner = new EventStoreRepository<InventoryItem>(eventStore, unitOfWork,
				InventoryItem.Factory, id => $"inventoryItem-{id}", messageTypeMapper,
				TransactoSerializerOptions.Events);
		}

		public void Add(InventoryItem inventoryItem) => _inner.Add(inventoryItem);
	}
}

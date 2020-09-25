using EventStore.Client;
using Transacto;
using Transacto.Framework;
using Transacto.Framework.CommandHandling;
using Transacto.Infrastructure;

namespace SomeCompany.Inventory {
	public class InventoryItemRepository {
		private readonly EventStoreRepository<InventoryItem> _inner;

		public InventoryItemRepository(EventStoreClient eventStore,
			IMessageTypeMapper messageTypeMapper, UnitOfWork unitOfWork) {
			_inner = new EventStoreRepository<InventoryItem>(eventStore, unitOfWork, InventoryItem.Factory,
				messageTypeMapper, TransactoSerializerOptions.Events);
		}

		public void Add(InventoryItem inventoryItem) => _inner.Add(inventoryItem);
	}
}

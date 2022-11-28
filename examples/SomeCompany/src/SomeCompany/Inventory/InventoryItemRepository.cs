using EventStore.Client;
using Transacto;
using Transacto.Framework;
using Transacto.Infrastructure.EventStore;

namespace SomeCompany.Inventory;

public class InventoryItemRepository {
	private readonly EventStoreRepository<InventoryItem> _inner;

	public InventoryItemRepository(EventStoreClient eventStore, IMessageTypeMapper messageTypeMapper) {
		_inner = new EventStoreRepository<InventoryItem>(eventStore, messageTypeMapper,
			TransactoSerializerOptions.Events);
	}

	public void Add(InventoryItem inventoryItem) => _inner.Add(inventoryItem);
}

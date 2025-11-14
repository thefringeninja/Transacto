using EventStore.Client;
using Transacto;
using Transacto.Framework;
using Transacto.Infrastructure.EventStore;

namespace SomeCompany.Inventory;

public class InventoryItemRepository(EventStoreClient eventStore, IMessageTypeMapper messageTypeMapper) {
	private readonly EventStoreRepository<InventoryItem> _inner = new(eventStore, messageTypeMapper,
		TransactoSerializerOptions.Events);

	public void Add(InventoryItem inventoryItem) => _inner.Add(inventoryItem);
}

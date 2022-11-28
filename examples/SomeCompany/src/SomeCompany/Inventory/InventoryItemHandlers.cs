namespace SomeCompany.Inventory;

public class InventoryItemHandlers {
	private readonly InventoryItemRepository _inventoryItems;

	public InventoryItemHandlers(InventoryItemRepository inventoryItems) {
		_inventoryItems = inventoryItems;
	}

	public ValueTask Handle(DefineInventoryItem command, in CancellationToken ct) {
		_inventoryItems.Add(InventoryItem.Define(new InventoryItemIdentifier(command.InventoryItemId),
			new Sku(command.Sku)));

		return new ValueTask(Task.CompletedTask);
	}
}

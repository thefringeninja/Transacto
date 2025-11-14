namespace SomeCompany.Inventory;

public class InventoryItemHandlers(InventoryItemRepository inventoryItems) {
	public ValueTask Handle(DefineInventoryItem command, in CancellationToken ct) {
		inventoryItems.Add(InventoryItem.Define(new InventoryItemIdentifier(command.InventoryItemId),
			new Sku(command.Sku)));

		return new ValueTask(Task.CompletedTask);
	}
}

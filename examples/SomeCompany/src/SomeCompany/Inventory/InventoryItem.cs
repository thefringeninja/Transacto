using Transacto.Framework;

namespace SomeCompany.Inventory;

public class InventoryItem : AggregateRoot, IAggregateRoot<InventoryItem> {
	public static InventoryItem Factory() => new();

	public InventoryItemIdentifier Identifier { get; private set; }

	public override string Id => FormatStreamName(Identifier);

	public static string FormatStreamName(InventoryItemIdentifier identifier) => $"inventoryItem-{identifier}";

	private InventoryItem() {
	}

	public static InventoryItem Define(InventoryItemIdentifier identifier, Sku sku) {
		var inventoryItem = new InventoryItem();

		inventoryItem.Apply(new InventoryItemDefined {
			Sku = sku.ToString(),
			InventoryItemId = identifier.ToGuid()
		});

		return inventoryItem;
	}

	protected override void ApplyEvent(object e) {
		if (e is InventoryItemDefined d) {
			Identifier = new InventoryItemIdentifier(d.InventoryItemId);
		}
	}
}

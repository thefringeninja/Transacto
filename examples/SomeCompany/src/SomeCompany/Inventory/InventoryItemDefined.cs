#nullable enable
namespace SomeCompany.Inventory;

public class InventoryItemDefined {
	public required Guid InventoryItemId { get; init; }
	public required string Sku { get; init; }
}

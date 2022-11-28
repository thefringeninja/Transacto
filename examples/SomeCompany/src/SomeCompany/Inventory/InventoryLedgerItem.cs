namespace SomeCompany.Inventory;

public record InventoryLedgerItem {
	public Guid InventoryItemId { get; init; }
	public required string Sku { get; init; }
	public required decimal OnOrder { get; init; }
	public required decimal OnHand { get; init; }
	public required decimal Committed { get; init; }
	public decimal Available => OnHand - Committed;
}

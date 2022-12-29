using System.Collections.Immutable;

namespace SomeCompany.PurchaseOrders;

public record PurchaseOrderPlaced {
	public Guid PurchaseOrderId { get; init; }
	public int PurchaseOrderNumber { get; init; }
	public Guid VendorId { get; init; }
	public ImmutableArray<PurchaseOrderItem> Items { get; init; } = ImmutableArray<PurchaseOrderItem>.Empty;

	public record PurchaseOrderItem {
		public Guid InventoryItemId { get; init; }
		public decimal Quantity { get; init; }
		public decimal UnitPrice { get; init; }
	}
}

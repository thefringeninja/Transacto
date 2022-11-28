using System.Collections.Immutable;

namespace SomeCompany.ReceiptOfGoods;

public record GoodsReceived {
	public Guid ReceiptId { get; init; }
	public int ReceiptNumber { get; init; }
	public ImmutableArray<ReceiptItem> Items { get; init; } = ImmutableArray<ReceiptItem>.Empty;

	public class ReceiptItem {
		public Guid InventoryItemId { get; init; }
		public decimal Quantity { get; init; }
		public decimal UnitPrice { get; init; }
	}
}

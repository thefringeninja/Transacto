using System;
using Transacto;

namespace SomeCompany.PurchaseOrders {
	public class PurchaseOrderFeedEntry : FeedEntry {
		public Guid PurchaseOrderId { get; set; }
		public Guid VendorId { get; set; }
		public int PurchaseOrderNumber { get; set; }
		public Item[] Items { get; set; } = Array.Empty<Item>();

		public class Item {
			public Guid InventoryItemId { get; set; }
			public decimal Quantity { get; set; }
			public decimal UnitPrice { get; set; }
		}
	}
}

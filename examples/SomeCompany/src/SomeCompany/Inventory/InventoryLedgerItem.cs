using System;

#nullable enable
namespace SomeCompany.Inventory {
	public class InventoryLedgerItem {
		public Guid InventoryItemId { get; set; }
		public string? Sku { get; set; }
		public decimal OnOrder { get; set; }
		public decimal OnHand { get; set; }
		public decimal Committed { get; set; }
		public decimal Available => OnHand - Committed;
	}
}

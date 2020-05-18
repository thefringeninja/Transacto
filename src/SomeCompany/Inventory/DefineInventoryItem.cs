using System;

namespace SomeCompany.Inventory {
	public class DefineInventoryItem {
		public Guid InventoryItemId { get; set; }
		public string Sku { get; set; } = null!;
	}
}

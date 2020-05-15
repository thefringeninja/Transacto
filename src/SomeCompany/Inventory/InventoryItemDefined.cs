using System;

#nullable enable
namespace SomeCompany.Inventory {
    public class InventoryItemDefined {
        public Guid InventoryItemId { get; set; }
        public string Sku { get; set; } = null!;
    }
}

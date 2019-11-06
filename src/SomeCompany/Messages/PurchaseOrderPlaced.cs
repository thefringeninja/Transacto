using System;

namespace Newtonsoft.Json.Messages {
    public class PurchaseOrderPlaced {
        public Guid PurchaseOrderId { get; set; }
        public int PurchaseOrderNumber { get; set; }
        public PurchaseOrderItem[] Items { get; set; } = Array.Empty<PurchaseOrderItem>();

        public class PurchaseOrderItem {
            public Guid InventoryItemId { get; set; }
            public double Quantity { get; set; }
            public decimal UnitPrice { get; set; }
        }
    }
}

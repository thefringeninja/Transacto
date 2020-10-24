using System;

namespace SomeCompany.ReceiptOfGoods {
    public class GoodsReceived {
        public Guid ReceiptId { get; set; }
        public int ReceiptNumber { get; set; }
        public ReceiptItem[] Items { get; set; } = Array.Empty<ReceiptItem>();

        public class ReceiptItem {
            public Guid InventoryItemId { get; set; }
            public decimal Quantity { get; set; }
            public decimal UnitPrice { get; set; }
        }
    }
}

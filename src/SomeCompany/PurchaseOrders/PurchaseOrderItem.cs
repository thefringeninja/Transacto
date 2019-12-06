using System;

namespace SomeCompany.PurchaseOrders {
    partial class PurchaseOrderItem {
        public decimal Total => Convert.ToDecimal(Quantity) * Convert.ToDecimal(UnitPrice);
    }
}

using System;

namespace SomeCompany.PurchaseOrders {
    partial class PurchaseOrderItems {
        public decimal Total => Convert.ToDecimal(Quantity) * Convert.ToDecimal(UnitPrice);
    }
}

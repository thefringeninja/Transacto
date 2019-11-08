using System;

namespace SomeCompany.ReceiptOfGoods {
    partial class ReceiptOfGoodsItems {
        public decimal Total => Convert.ToDecimal((double)Quantity) * Convert.ToDecimal((double)UnitPrice);
    }
}

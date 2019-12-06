using System;

namespace SomeCompany.ReceiptOfGoods {
    partial class ReceiptOfGoodsItem {
        public decimal Total => Convert.ToDecimal(Quantity) * Convert.ToDecimal(UnitPrice);
    }
}

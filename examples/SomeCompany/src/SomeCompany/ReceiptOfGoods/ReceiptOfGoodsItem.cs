namespace SomeCompany.ReceiptOfGoods;

partial record ReceiptOfGoodsItem {
	public decimal Total => Convert.ToDecimal(Quantity) * Convert.ToDecimal(UnitPrice);
}

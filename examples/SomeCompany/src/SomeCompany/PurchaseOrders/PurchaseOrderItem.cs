namespace SomeCompany.PurchaseOrders;

partial record PurchaseOrderItem {
	public decimal Total => Convert.ToDecimal(Quantity) * Convert.ToDecimal(UnitPrice);
}

namespace Transacto.Plugins.BalanceSheet; 

partial record LineItem {
	public LineItem() {
		Name = null!;
		Balance = new Decimal();
	}
}
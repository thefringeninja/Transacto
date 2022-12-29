namespace Transacto.Plugins.BalanceSheet; 

partial record LineItemGrouping {
	public decimal Total => LineItems.Sum(x => x.Balance.DecimalValue);
}

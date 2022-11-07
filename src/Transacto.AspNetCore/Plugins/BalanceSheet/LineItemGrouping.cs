using System.Collections.Immutable;

namespace Transacto.Plugins.BalanceSheet; 

partial record LineItemGrouping {
	public LineItemGrouping() {
		LineItems = ImmutableArray<LineItem>.Empty;
		LineItemGroupings = ImmutableArray<LineItemGrouping>.Empty;
	}
	public decimal Total => LineItems.Sum(x => x.Balance.DecimalValue);
}

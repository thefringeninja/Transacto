using System.Collections.Immutable;
using System.Linq;

namespace Transacto.Plugins.BalanceSheet {
	partial record LineItemGrouping {
		public LineItemGrouping() {
			LineItems = ImmutableArray<LineItem>.Empty;
			LineItemGroupings = ImmutableArray<LineItemGrouping>.Empty;
		}
		public decimal Total => LineItems.Sum(x => x.Balance.DecimalValue);
	}
}

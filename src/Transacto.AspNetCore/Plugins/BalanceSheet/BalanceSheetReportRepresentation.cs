using System.Collections.Generic;
using Hallo;

namespace Transacto.Plugins.BalanceSheet {
	internal class BalanceSheetReportRepresentation : Hal<BalanceSheetReport>, IHalLinks<BalanceSheetReport>,
		IHalState<BalanceSheetReport> {
		public IEnumerable<Link> LinksFor(BalanceSheetReport resource) {
			yield break;
		}

		public object StateFor(BalanceSheetReport resource) => resource;
	}
}

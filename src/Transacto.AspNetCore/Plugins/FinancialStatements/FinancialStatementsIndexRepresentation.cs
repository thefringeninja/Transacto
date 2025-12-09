using Hallo;
using NodaTime.Text;

namespace Transacto.Plugins.FinancialStatements;

public class FinancialStatementsIndexRepresentation : Hal<FinancialStatementsIndex>,
	IHalState<FinancialStatementsIndex>,
	IHalLinks<FinancialStatementsIndex> {
	public IEnumerable<Link> LinksFor(FinancialStatementsIndex resource) {
		yield return new Link("self", "financial-statements", title: "Index");
		yield return new Link("index", "financial-statements", title: "Index");

		var startOfYear = LocalDatePattern.Iso.Format(resource.StartOfYear);
		yield return new Link("t:financial-statement",
			$"financial-statements/balance-sheet?thru={startOfYear}", title: "Balance Sheet Report");
		yield return new Link("t:financial-statement",
			$"financial-statements/cash-flow?startingOn={startOfYear}&period=P1Y", title: "Cash Flow");
		yield return new Link("t:financial-statement",
			$"financial-statements/profit-and-loss?startingOn={startOfYear}&period=P1Y", title: "Profit and Loss");
	}

	public object StateFor(FinancialStatementsIndex resource) => resource;
}
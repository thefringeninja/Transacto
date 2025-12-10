using Hallo;
using NodaTime.Text;
using Transacto.Framework.Http;

namespace Transacto.Plugins.FinancialStatements;

public class FinancialStatementsIndexRepresentation(SiteRoot siteRoot) : Hal<FinancialStatementsIndex>,
	IHalState<FinancialStatementsIndex>,
	IHalLinks<FinancialStatementsIndex> {
	public IEnumerable<Link> LinksFor(FinancialStatementsIndex resource) {
		yield return new Link("self", $"{siteRoot.Uri.AbsolutePath}", title: "Financial Statements");
		yield return new Link("index", "financial-statements", title: "Financial Statements");

		var startOfYear = LocalDatePattern.Iso.Format(resource.StartOfYear);
		yield return new Link("t:financial-statement",
			$"{siteRoot.Uri.AbsolutePath}/balance-sheet?thru={startOfYear}", title: "Balance Sheet");
		yield return new Link("t:financial-statement",
			$"{siteRoot.Uri.AbsolutePath}/cash-flow?startingOn={startOfYear}&period=P1Y", title: "Cash Flow");
		yield return new Link("t:financial-statement",
			$"{siteRoot.Uri.AbsolutePath}/profit-and-loss?startingOn={startOfYear}&period=P1Y",
			title: "Profit and Loss");
	}

	public object StateFor(FinancialStatementsIndex resource) => resource;
}

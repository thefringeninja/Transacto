using Hallo;
using Transacto.Framework.Http.Html;

namespace Transacto.Plugins.FinancialStatements.CashFlow;

internal class CashFlowReportsRepresentation(SiteRoot siteRoot)
	: Hal<CashFlowReports>, IHalState<CashFlowReports>, IHalLinks<CashFlowReports> {
	public object StateFor(CashFlowReports resource) => resource;

	public IEnumerable<Link> LinksFor(CashFlowReports resource) {
		yield return new Link("self",
			$"{siteRoot.Uri.AbsolutePath}/cash-flow?{ReportRange.ToFormUrlEncoded(resource.Groups.Select(group => group.ReportRange))}",
			title: $"Cash Flow ({string.Join(", ", resource.Groups.Select(group => group.ReportRange))})");

		yield return new Link("index", siteRoot.Uri.AbsolutePath, title: "Financial Statements");
	}
}

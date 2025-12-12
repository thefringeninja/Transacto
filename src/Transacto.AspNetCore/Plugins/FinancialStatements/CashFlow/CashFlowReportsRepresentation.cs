using Hallo;
using NodaTime;
using Transacto.Framework.Http.Html;

namespace Transacto.Plugins.FinancialStatements.CashFlow;

internal class CashFlowReportsRepresentation(SiteRoot siteRoot, GetFormatSettings getFormatSettings)
	: Hal<CashFlowReports>, IHalState<CashFlowReports>, IHalLinks<CashFlowReports> {
	
	public object StateFor(CashFlowReports resource) => resource;

	public IEnumerable<Link> LinksFor(CashFlowReports resource) {
		yield return new Link("index", "/", title: "Home");
		
		yield return new Link("index", siteRoot.Uri.AbsolutePath, title: "Financial Statements");
		
		var now = LocalDate.FromDateTime(DateTime.Now);
		var firstOfYear = now.PlusDays(-now.DayOfYear + 1);
		var current = resource.Groups.Select(group => group.ReportRange).ToArray();
		var currentEncoded = ReportRange.ToFormUrlEncoded(current);

		yield return new Link("self",
			$"{siteRoot.Uri.AbsolutePath}/cash-flow?{currentEncoded}",
			title: $"Cash Flow ({string.Join(", ", current)})");

		yield return new Link(FinancialStatements.Relation,
			$"{siteRoot.Uri.AbsolutePath}/cash-flow?{currentEncoded}",
			title: $"Cash Flow ({string.Join(", ", current)})");

		if (resource.Groups.Length > 0) {
			var (startingOn, period) = resource.Groups[0].ReportRange;
			var withLastPeriod = $"{currentEncoded}&{new ReportRange(
				period.Days == 0 ? startingOn.Minus(period) : firstOfYear, period).ToFormUrlEncoded()}";

			yield return new Link(FinancialStatements.Relation,
				$"{siteRoot.Uri.AbsolutePath}/cash-flow?{withLastPeriod}",
				title: "Compare with last period");
			
			if (period.Days != 0) {
				yield break;
			}

			var prev = new ReportRange(startingOn.Minus(period), period);
			var next = new ReportRange(startingOn.Plus(period), period);
			var formatSettings = getFormatSettings();
			yield return new Link(
				"prev",
				$"{siteRoot.Uri.AbsolutePath}/cash-flow?{prev.ToFormUrlEncoded()}",
				title: $"{formatSettings.ShortDatePattern.Format(prev.StartingOn)}");
			yield return new Link(
				"next",
				$"{siteRoot.Uri.AbsolutePath}/cash-flow?{next.ToFormUrlEncoded()}",
				title: $"{formatSettings.ShortDatePattern.Format(next.StartingOn)}");

		}

		yield return new Link(
			FinancialStatements.Relation2,
			$"{siteRoot.Uri.AbsolutePath}/cash-flow?starting-on={{starting-on}}&period={{period}}",
			title: "Run Cash Flow");

		yield return new Link(
			FinancialStatements.Relation,
			$"{siteRoot.Uri.AbsolutePath}/cash-flow?{new ReportRange(firstOfYear, Period.FromDays(now.DayOfYear)).ToFormUrlEncoded()}",
			title: "Cash Flow YTD");
		

	}
}

using Hallo;
using NodaTime;
using Transacto.Framework.Http.Html;

namespace Transacto.Plugins.FinancialStatements.ProfitAndLoss;

internal class ProfitAndLossReportsRepresentation(SiteRoot siteRoot)
	: Hal<ProfitAndLossReports>, IHalState<ProfitAndLossReports>, IHalLinks<ProfitAndLossReports> {
	public object StateFor(ProfitAndLossReports resource) => resource;

	public IEnumerable<Link> LinksFor(ProfitAndLossReports resource) {
		if (resource.Groups.Length == 0) {
			yield break;
		}

		var current = LocalDate.FromDateTime(DateTime.Now);

		yield return new Link(
			"self",
			$"{siteRoot.Uri.AbsolutePath}/profit-and-loss?{ReportRange.ToFormUrlEncoded(resource.Groups.Select(group => group.ReportRange))}",
			title: $"Profit and Loss ({string.Join(", ", resource.Groups.Select(group => group.ReportRange))})");


		var firstOfYear = current.PlusDays(-current.DayOfYear + 1);
		for (var i = 0; i < 4; i++) {
			var quarterly = firstOfYear.PlusMonths(i * 3);

			var reportRange = new ReportRange(quarterly, Period.FromMonths(3));
			yield return new Link(
				"self",
				$"{siteRoot.Uri.AbsolutePath}/profit-and-loss?{reportRange.ToFormUrlEncoded()}",
				title: $"Profit and Loss Q{GetQuarter(quarterly)} {quarterly.Year}");
		}

		yield return new Link(
			"self",
			$"{siteRoot.Uri.AbsolutePath}/profit-and-loss?{new ReportRange(firstOfYear, Period.FromYears(1)).ToFormUrlEncoded()}",
			title: $"Profit and Loss {current.Year}");

		yield return new Link(
			"self",
			$"{siteRoot.Uri.AbsolutePath}/profit-and-loss?{new ReportRange(firstOfYear, Period.FromDays(current.DayOfYear)).ToFormUrlEncoded()}",
			title: $"Profit and Loss YTD");

		yield return new Link(
			"self",
			$"{siteRoot.Uri.AbsolutePath}/profit-and-loss?starting-on={{starting-on}}&period={{period}}",
			title: $"Profit and Loss YTD");
		yield return new Link("index", siteRoot.Uri.AbsolutePath, title: "Financial Statements");
	}

	private static int GetQuarter(LocalDate date) => date.Month switch {
		>= 1 and < 4 => 1,
		>= 4 and < 7 => 2,
		>= 7 and < 10 => 3,
		_ => 4
	};
}

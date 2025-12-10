using Hallo;
using NodaTime;
using NodaTime.Text;
using Transacto.Framework.Http;

namespace Transacto.Plugins.FinancialStatements.BalanceSheet;

public class BalanceSheetReportsRepresentation(SiteRoot siteRoot) : Hal<BalanceSheetReports>,
	IHalLinks<BalanceSheetReports>,
	IHalState<BalanceSheetReports> {
	public IEnumerable<Link> LinksFor(BalanceSheetReports resource) {
		if (resource.Groups.Length == 0) {
			yield break;
		}

		var report = resource.Groups[0];
		var includeAll = report.Thru == LocalDate.MaxIsoValue;
		var current = includeAll ? LocalDate.FromDateTime(DateTime.Now) : report.Thru;

		yield return new Link(
			"self",
			$"{siteRoot.Uri.AbsolutePath}/balance-sheet?thru={LocalDatePattern.Iso.Format(report.Thru)}",
			title: includeAll ? "Balance Sheet" : $"Balance Sheet thru {current}");


		for (var i = 0; i < 4; i++) {
			var quarterly = current
				.PlusDays(-current.DayOfYear + 1)
				.PlusMonths((i * 3) + 3)
				.PlusDays(-1);

			yield return new Link(
				"self",
				$"{siteRoot.Uri.AbsolutePath}/balance-sheet?thru={LocalDatePattern.Iso.Format(quarterly)}",
				title: $"Balance Sheet thru Q{GetQuarter(quarterly)} {quarterly.Year}");
		}
	}

	public object StateFor(BalanceSheetReports resource) => resource;

	private static int GetQuarter(LocalDate date) => date.Month switch {
		>= 1 and < 4 => 1,
		>= 4 and < 7 => 2,
		>= 7 and < 10 => 3,
		_ => 4
	};
}

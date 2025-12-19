using Hallo;
using NodaTime;
using NodaTime.Text;
using Transacto.Domain;
using Transacto.Framework.Http.Html;
using Transacto.Plugins.GeneralLedger;

namespace Transacto.Plugins.FinancialStatements.BalanceSheet;

public class BalanceSheetReportsRepresentation(SiteRoot siteRoot, GetFormatter getFormatter)
	: Hal<BalanceSheetReports>, IHalLinks<BalanceSheetReports>, IHalState<BalanceSheetReports> {
	private readonly Formatter _formatter = getFormatter();

	public IEnumerable<Link> LinksFor(BalanceSheetReports balanceSheetReports) {
		var now = LocalDate.FromDateTime(DateTime.Now);

		yield return new Link("index", "/", title: "Home");

		yield return new Link("index", siteRoot.Uri.AbsolutePath, title: "Financial Statements");

		yield return new Link(
			FinancialStatements.Relation,
			$"{siteRoot.Uri.AbsolutePath}/balance-sheet?thru={LocalDatePattern.Iso.Format(now)}",
			title: "Balance Sheet (Fiscal YTD)");

		yield return new Link(
			FinancialStatements.Relation2,
			$"{siteRoot.Uri.AbsolutePath}/balance-sheet?thru={{thru}}",
			title: "Run Balance Sheet");


		if (balanceSheetReports.Groups.Length == 0) {
			yield break;
		}

		var report = balanceSheetReports.Groups[0];
		var includeAll = report.Thru == LocalDate.MaxIsoValue;
		var start = includeAll ? now : report.Thru;

		var currentEncoded = string.Join('&',
			balanceSheetReports.Groups.Select(group => $"thru={LocalDatePattern.Iso.Format(group.Thru)}"));

		yield return new Link(FinancialStatements.Relation,
			$"{siteRoot.Uri.AbsolutePath}/balance-sheet?{currentEncoded}",
			title: "Balance Sheet");
		yield return new Link("self",
			$"{siteRoot.Uri.AbsolutePath}/balance-sheet?{currentEncoded}",
			title: "Balance Sheet");

		for (var i = 0; i < 4; i++) {
			var quarterly = start
				.PlusDays(-start.DayOfYear + 1)
				.PlusMonths(i * 3 + 3)
				.PlusDays(-1);

			yield return new Link(
				FinancialStatements.Relation,
				$"{siteRoot.Uri.AbsolutePath}/balance-sheet?thru={LocalDatePattern.Iso.Format(quarterly)}",
				title: $"Q{GetQuarter(quarterly)} {quarterly.Year}");
		}

		(LocalDate, string)[] prev = [
			(new LocalDate(report.Thru.Year, 1, 1).PlusDays(-1), "Last Year"),
			(report.Thru.FirstDayOfNextQuarter.Plus(-Period.Quarterly).PlusDays(-1), "Previous Quarter"),
			(report.Thru.ToYearMonth().OnDayOfMonth(1).PlusDays(-1), "Last Month"),
			(report.Thru.StartOfThisWeek, "Last Week"),
			(report.Thru.PlusDays(-1), "Yesterday"),
		];

		foreach (var (link, title) in prev) {
			yield return new Link(
				"prev",
				$"{siteRoot.Uri.AbsolutePath}/balance-sheet?thru={_formatter.ShortDatePattern.Format(link)}",
				title: title);
		}

		(LocalDate, string)[] nextLinks = [
			(report.Thru.PlusDays(1), "Tomorrow"),
			(report.Thru.StartOfThisWeek.Plus(Period.Weekly), "Next Week"),
			(report.Thru.PlusMonths(2).ToYearMonth().OnDayOfMonth(1).PlusDays(-1), "Next Month"),
			(report.Thru.FirstDayOfNextQuarter.Plus(Period.Quarterly).PlusDays(-1), "Next Quarter"),
			(new LocalDate(report.Thru.PlusYears(1).Year, 1, 1).PlusDays(-1).PlusYears(1), "Next Year"),
		];

		foreach (var (next, title) in nextLinks) {
			yield return new Link(
				"next",
				$"{siteRoot.Uri.AbsolutePath}/balance-sheet?thru={_formatter.ShortDatePattern.Format(next)}",
				title: title);
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

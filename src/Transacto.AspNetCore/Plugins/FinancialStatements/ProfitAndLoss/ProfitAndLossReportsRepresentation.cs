using Hallo;
using NodaTime;
using Transacto.Domain;
using Transacto.Framework.Http.Html;
using Transacto.Plugins.GeneralLedger;

namespace Transacto.Plugins.FinancialStatements.ProfitAndLoss;

internal class ProfitAndLossReportsRepresentation(SiteRoot siteRoot, GetFormatter getFormatter)
	: Hal<ProfitAndLossReports>, IHalState<ProfitAndLossReports>, IHalLinks<ProfitAndLossReports> {
	private readonly Formatter _formatter = getFormatter();
	public object StateFor(ProfitAndLossReports resource) => resource;

	public IEnumerable<Link> LinksFor(ProfitAndLossReports resource) {
		var now = LocalDate.FromDateTime(DateTime.Now);
		var firstOfYear = now.PlusDays(-now.DayOfYear + 1);

		yield return new Link("index", "/", title: "Home");

		yield return new Link("index", siteRoot.Uri.AbsolutePath, title: "Financial Statements");

		for (var i = 0; i < 4; i++) {
			var quarterly = firstOfYear.PlusMonths(i * 3);

			var reportRange = new ReportRange(quarterly, Period.Quarterly);
			yield return new Link(
				FinancialStatements.Relation,
				$"{siteRoot.Uri.AbsolutePath}/profit-and-loss?{reportRange.ToFormUrlEncoded()}",
				title: $"Profit and Loss Q{GetQuarter(quarterly)} {quarterly.Year}");
		}

		yield return new Link(
			FinancialStatements.Relation,
			$"{siteRoot.Uri.AbsolutePath}/profit-and-loss?{new ReportRange(firstOfYear, Period.Yearly).ToFormUrlEncoded()}",
			title: $"Profit and Loss {now.Year}");

		yield return new Link(
			FinancialStatements.Relation,
			$"{siteRoot.Uri.AbsolutePath}/profit-and-loss?{new ReportRange(firstOfYear, Period.FromDays(now.DayOfYear)).ToFormUrlEncoded()}",
			title: "Profit and Loss YTD");

		yield return new Link(
			FinancialStatements.Relation2,
			$"{siteRoot.Uri.AbsolutePath}/profit-and-loss?starting-on={{starting-on}}&period={{period}}",
			title: "Run Profit and Loss");

		if (resource.Groups.Length == 0) {
			yield break;
		}

		var current = resource.Groups.Select(group => group.ReportRange).ToArray();
		var currentEncoded = ReportRange.ToFormUrlEncoded(current);
		var (startingOn, period) = resource.Groups[0].ReportRange;
		var withLastPeriod = $"{currentEncoded}&{new ReportRange(
			period.Days == 0 ? startingOn.Minus(period) : firstOfYear, period).ToFormUrlEncoded()}";

		yield return new Link(
			"self",
			$"{siteRoot.Uri.AbsolutePath}/profit-and-loss?{currentEncoded}",
			title: $"Profit and Loss ({string.Join(", ", current)})");

		yield return new Link(
			FinancialStatements.Relation,
			$"{siteRoot.Uri.AbsolutePath}/profit-and-loss?{currentEncoded}",
			title: $"Profit and Loss ({string.Join(", ", current)})");

		yield return new Link(
			FinancialStatements.Relation,
			$"{siteRoot.Uri.AbsolutePath}/profit-and-loss?{withLastPeriod}",
			title: "Compare to Last Period");


		if (period.Days != 0) {
			yield break;
		}

		var prev = new ReportRange(startingOn.Minus(period), period);
		var next = new ReportRange(startingOn.Plus(period), period);
		yield return new Link(
			"prev",
			$"{siteRoot.Uri.AbsolutePath}/profit-and-loss?{prev.ToFormUrlEncoded()}",
			title: $"Profit and Loss {_formatter.ShortDatePattern.Format(prev.StartingOn)}");
		yield return new Link(
			"next",
			$"{siteRoot.Uri.AbsolutePath}/profit-and-loss?{next.ToFormUrlEncoded()}",
			title: $"Profit and Loss {_formatter.ShortDatePattern.Format(next.StartingOn)}");
	}

	private static int GetQuarter(LocalDate date) => date.Month switch {
		>= 1 and < 4 => 1,
		>= 4 and < 7 => 2,
		>= 7 and < 10 => 3,
		_ => 4
	};
}

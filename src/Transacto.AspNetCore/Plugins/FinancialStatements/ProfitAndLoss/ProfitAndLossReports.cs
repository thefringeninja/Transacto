using System.Collections.Immutable;

namespace Transacto.Plugins.FinancialStatements.ProfitAndLoss;

public record ProfitAndLossReports(
	ImmutableArray<ReportGroup> Groups) {
	public AccountMap AccountMap {
		get => field ??= new(
			GetAccountLookup(g => g.GrossRevenue),
			GetAccountLookup(g => g.CostOfRevenue),
			GetAccountLookup(g => g.OperatingExpenses),
			GetAccountLookup(g => g.OtherExpenses),
			GetAccountLookup(g => g.OtherIncome));
	}

	private ImmutableSortedDictionary<int, string> GetAccountLookup(Func<ReportGroup, Report> getReport) => Groups
		.SelectMany(group => getReport(group).Accounts).Distinct().ToImmutableSortedDictionary(x => x.Number, x => x.Name);
}

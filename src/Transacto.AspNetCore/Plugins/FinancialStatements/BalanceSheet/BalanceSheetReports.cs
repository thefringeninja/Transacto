using System.Collections.Immutable;

namespace Transacto.Plugins.FinancialStatements.BalanceSheet;

public sealed record BalanceSheetReports(ImmutableArray<ReportGroup> Groups) {
	public AccountMap AccountMap {
		get => field ?? new(
			GetAccountLookup(g => g.Assets),
			GetAccountLookup(g => g.Liabilities),
			GetAccountLookup(g => g.Equity));
	}
	
	private ImmutableSortedDictionary<int, string> GetAccountLookup(Func<ReportGroup, Report> getReport) => Groups
		.SelectMany(group => getReport(group).Accounts).Distinct().ToImmutableSortedDictionary(x => x.Number, x => x.Name);

}

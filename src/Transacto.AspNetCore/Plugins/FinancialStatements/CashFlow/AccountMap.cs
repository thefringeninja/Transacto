using System.Collections.Immutable;

namespace Transacto.Plugins.FinancialStatements.CashFlow;

public sealed record AccountMap(
	ImmutableSortedDictionary<int, string> Operations,
	ImmutableSortedDictionary<int, string> Investment,
	ImmutableSortedDictionary<int, string> Finance) {
	public static AccountMap From(ImmutableArray<ReportGroup> groups) => new(
		GetAccountLookup(groups, group => group.Operations),
		GetAccountLookup(groups, group => group.Investment),
		GetAccountLookup(groups, group => group.Finance));

	private static ImmutableSortedDictionary<int, string> GetAccountLookup(ImmutableArray<ReportGroup> groups,
		Func<ReportGroup, Report> getReport) => groups
		.SelectMany(group => getReport(group).Accounts)
		.Distinct()
		.ToImmutableSortedDictionary(x => x.Number, x => x.Name);
}

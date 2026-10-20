using System.Collections.Immutable;

namespace Transacto.Plugins.FinancialStatements.ProfitAndLoss;

public record AccountMap(
	ImmutableSortedDictionary<int, string> GrossRevenue,
	ImmutableSortedDictionary<int, string> CostOfRevenue,
	ImmutableSortedDictionary<int, string> OperatingExpenses,
	ImmutableSortedDictionary<int, string> OtherExpenses,
	ImmutableSortedDictionary<int, string> OtherIncome);
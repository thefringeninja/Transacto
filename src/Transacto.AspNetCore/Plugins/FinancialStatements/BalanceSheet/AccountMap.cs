using System.Collections.Immutable;

namespace Transacto.Plugins.FinancialStatements.BalanceSheet;

public record AccountMap(
	ImmutableSortedDictionary<int, string> Assets,
	ImmutableSortedDictionary<int, string> Liability,
	ImmutableSortedDictionary<int, string> Equity);

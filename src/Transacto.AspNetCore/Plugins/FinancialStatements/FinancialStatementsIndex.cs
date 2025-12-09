using NodaTime;

namespace Transacto.Plugins.FinancialStatements;

public readonly record struct FinancialStatementsIndex(LocalDate StartOfYear) {
	public LocalDate EndOfYear => StartOfYear.PlusYears(1);
}
using System.Collections.Immutable;
using NodaTime;
using Transacto.Domain;

namespace Transacto.Plugins.FinancialStatements.BalanceSheet;

public sealed record ReportGroup(
	LocalDate Thru,
	Report.Assets Assets,
	Report.Liabilities Liabilities,
	Report.Equity Equity,
	decimal IncomeSummary) {
	public decimal TotalEquity => IncomeSummary + Equity.Total;
	
	public decimal TotalEquityAndLiabilities => TotalEquity + Liabilities.Total;

	public static ReportGroup Create(LocalDate thru, ImmutableArray<Report.Line> lines) =>
		new(thru, new Report.Assets(lines), new Report.Liabilities(lines), new Report.Equity(lines),
			lines.Sum(line => Account.For(new AccountNumber(line.Account.Number)) switch {
				IncomeAccount => line.Balance,
				ExpenseAccount => -line.Balance,
				_ => 0m
			}));
}

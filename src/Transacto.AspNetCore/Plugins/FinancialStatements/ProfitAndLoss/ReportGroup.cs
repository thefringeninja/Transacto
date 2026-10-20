using System.Collections.Immutable;

namespace Transacto.Plugins.FinancialStatements.ProfitAndLoss;

public record ReportGroup(
	ReportRange ReportRange,
	Report.GrossRevenue GrossRevenue,
	Report.CostOfRevenue CostOfRevenue,
	Report.OperatingExpenses OperatingExpenses,
	Report.OtherExpenses OtherExpenses,
	Report.OtherIncome OtherIncome) {
	public decimal GrossProfit => GrossRevenue.Total - CostOfRevenue.Total;
	public decimal OperatingIncome => GrossProfit - OperatingExpenses.Total;
	public decimal NetProfit => OtherIncome.Total + OperatingIncome - OtherExpenses.Total;

	public static ReportGroup Create(ReportRange reportRange, ImmutableArray<Report.Line> lines) =>
		new(reportRange,
			new Report.GrossRevenue(reportRange, lines),
			new Report.CostOfRevenue(reportRange, lines),
			new Report.OperatingExpenses(reportRange, lines),
			new Report.OtherExpenses(reportRange, lines),
			new Report.OtherIncome(reportRange, lines));
}

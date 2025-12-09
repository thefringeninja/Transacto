using System.Collections.Immutable;

namespace Transacto.Plugins.FinancialStatements.CashFlow;

public record ReportGroup(
	ReportRange ReportRange,
	decimal StartingCash,
	decimal NetIncome,
	Report.Operations Operations,
	Report.Investment Investment,
	Report.Finance Finance) {
	public decimal CashFlow => Operations.Total + Investment.Total + Finance.Total;
	public decimal EndingCash => StartingCash + CashFlow;

	public static ReportGroup Create(ReportRange reportRange, decimal startingCash, decimal netIncome,
		ImmutableArray<Report.Line> lines) => new(reportRange, startingCash, netIncome,
		new Report.Operations(lines),
		new Report.Investment(lines),
		new Report.Finance(lines));
}

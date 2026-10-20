using System.Collections.Immutable;

namespace Transacto.Plugins.FinancialStatements.CashFlow;

public sealed record CashFlowReports(ImmutableArray<ReportGroup> Groups) {
	public AccountMap AccountMap => field ??= AccountMap.From(Groups);
}

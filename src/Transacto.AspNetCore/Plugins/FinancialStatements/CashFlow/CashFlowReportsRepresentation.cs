using Hallo;

namespace Transacto.Plugins.FinancialStatements.CashFlow;

internal class CashFlowReportsRepresentation : Hal<CashFlowReports>, IHalState<CashFlowReports> {
	public object StateFor(CashFlowReports resource) => resource;
}

using Hallo;

namespace Transacto.Plugins.FinancialStatements.ProfitAndLoss;

internal class ProfitAndLossReportsRepresentation : Hal<ProfitAndLossReports>, IHalState<ProfitAndLossReports> {
	public object StateFor(ProfitAndLossReports resource) => resource;
}

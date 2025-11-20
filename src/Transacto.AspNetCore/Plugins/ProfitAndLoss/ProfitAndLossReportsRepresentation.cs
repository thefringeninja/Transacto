using Hallo;

namespace Transacto.Plugins.ProfitAndLoss;

public class ProfitAndLossReportsRepresentation : Hal<ProfitAndLossReports>, IHalState<ProfitAndLossReports> {
	public object StateFor(ProfitAndLossReports resource) => resource;
}
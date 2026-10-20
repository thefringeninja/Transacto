using Hallo;

namespace Transacto.Plugins.GeneralLedger;

public class CurrentPeriodRepresentation : Hal<CurrentPeriod>, IHalState<CurrentPeriod> {
	public object StateFor(CurrentPeriod resource) => resource;
}

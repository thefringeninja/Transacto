using Hallo;

namespace Transacto.Framework.Projections;

public enum ProjectionStatus {
	CatchingUp,
	Live,
	Faulted
}

public class ProjectionStateRepresentation : Hal<Dictionary<string, ProjectionState>>,
	IHalState<Dictionary<string, ProjectionState>> {
	public object StateFor(Dictionary<string, ProjectionState> resource) => resource;
}

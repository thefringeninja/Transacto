using Hallo;

namespace Transacto.Framework.Projections;

public enum ProjectionStatus {
	CatchingUp,
	Live,
	Faulted
}

public class ProjectionsControllerRepresentation : Hal<ProjectionsController>,
	IHalState<ProjectionsController> {
	public object StateFor(ProjectionsController resource) => resource.State;
}

public class ProjectionStateRepresentation : Hal<ProjectionState>,
	IHalState<ProjectionState> {
	public object StateFor(ProjectionState resource) => resource;
}

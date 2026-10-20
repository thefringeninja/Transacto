namespace Transacto.Framework.Projections;

internal class ProjectionHostStatusWrapper<T>(T instance)
	: IProjectionHostStatus where T : class, IHostedService, IProjectionHostStatus {
	public IReadOnlyList<ProjectionState> State => [..instance.State.Select(PrefixName)];

	private ProjectionState PrefixName(ProjectionState state) =>
		state with { Name = state.Name };

	public bool TryGetState(string name, out ProjectionState state) {
		if (!instance.TryGetState(name, out state)) return false;
		state = PrefixName(state);
		return true;
	}
}

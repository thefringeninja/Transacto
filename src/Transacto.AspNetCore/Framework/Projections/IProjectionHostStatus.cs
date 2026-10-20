namespace Transacto.Framework.Projections;

public interface IProjectionHostStatus {
	public IReadOnlyList<ProjectionState> State { get; }
	public bool TryGetState(string name, out ProjectionState state);
}

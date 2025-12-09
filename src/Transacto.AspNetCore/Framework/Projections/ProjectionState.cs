using KurrentDB.Client;

namespace Transacto.Framework.Projections;

public readonly record struct ProjectionState(
	string Name,
	ulong Last,
	ulong? Current,
	ProjectionStatus Status,
	string? ErrorMessage = null) {
	public double Progress {
		get {
			if (Last == ulong.MaxValue) {
				return 0;
			}

			if (!Current.HasValue) {
				return 0;
			}

			if (Status == ProjectionStatus.Live) {
				return 1;
			}

			return Current.Value / (double)Last;
		}
	}

	public ProjectionState Checkpoint(FromAll checkpoint) =>
		this with {
			Current = checkpoint.ToUInt64().preparePosition,
			Status = ProjectionStatus.CatchingUp,
			ErrorMessage = null
		};

	public ProjectionState End(FromAll last) => this with { Last = last.ToUInt64().preparePosition };
	public static ProjectionState Named(string name) => new(name, ulong.MaxValue, null, ProjectionStatus.CatchingUp);
}

using System.Text.Json.Serialization;
using KurrentDB.Client;

namespace Transacto.Framework.Projections;

public readonly record struct ProjectionState(
	string Name,
	ulong Last,
	ulong? Current,
	[property:  JsonConverter(typeof(JsonStringEnumConverter))] ProjectionStatus Status,
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
			Current = checkpoint == FromAll.Start ? 0 : checkpoint.ToUInt64().preparePosition,
			Status = ProjectionStatus.CatchingUp,
			ErrorMessage = null
		};

	public ProjectionState End(FromAll last) =>
		this with { Last = last == FromAll.Start ? 0 : last.ToUInt64().preparePosition };

	public static ProjectionState Named(string name) => new(name, ulong.MaxValue, null, ProjectionStatus.CatchingUp);
}

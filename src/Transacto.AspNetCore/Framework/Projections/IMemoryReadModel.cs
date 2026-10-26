using KurrentDB.Client;

namespace Transacto.Framework.Projections;

public abstract record MemoryReadModel {
	public Option<Position> Position { get; init; } = Option<Position>.None;
}

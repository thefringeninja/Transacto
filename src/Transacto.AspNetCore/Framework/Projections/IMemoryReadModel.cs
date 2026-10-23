using KurrentDB.Client;

namespace Transacto.Framework.Projections;

public abstract record MemoryReadModel {
	public Optional<Position> Position { get; init; } = Optional<Position>.Empty;
}

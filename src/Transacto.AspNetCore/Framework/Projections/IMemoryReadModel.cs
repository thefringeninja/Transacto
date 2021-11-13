using EventStore.Client;

namespace Transacto.Framework.Projections; 

public abstract record MemoryReadModel {
	public Optional<Position> Checkpoint { get; init; } = Optional<Position>.Empty;
}
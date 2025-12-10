using System.Text.Json.Serialization;
using KurrentDB.Client;

namespace Transacto.Framework.Projections;

public abstract record MemoryReadModel {
	public Optional<Position> Checkpoint { get; init; } = Optional<Position>.Empty;
}

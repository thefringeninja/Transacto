using EventStore.Client;

namespace Transacto.Framework.Projections {
	public interface IMemoryReadModel {
		Optional<Position> Checkpoint { get; set; }
	}
}

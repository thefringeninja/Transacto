using EventStore.Client;
using Transacto.Framework;

namespace Transacto {
	public interface IMemoryReadModel {
		Optional<Position> Checkpoint { get; set; }
	}
}

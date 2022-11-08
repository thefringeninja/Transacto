using EventStore.Client;
using Transacto.Framework;

namespace Transacto; 

internal static class PositionExtensions {
	public static Checkpoint ToCheckpoint(this Optional<Position> position) =>
		!position.HasValue ? Checkpoint.None : position.Value.ToCheckpoint();

	public static Checkpoint ToCheckpoint(this Position position) {
		Span<byte> checkpoint = stackalloc byte[8];

		BitConverter.TryWriteBytes(checkpoint, position.CommitPosition);

		return new Checkpoint(checkpoint.ToArray());
	}
}

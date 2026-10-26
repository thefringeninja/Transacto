using KurrentDB.Client;
using Transacto.Framework;

namespace Transacto.Infrastructure.EventStore;

internal static class PositionExtensions {
	public static Checkpoint ToCheckpoint(this Optional<Position> position) =>
		!position.HasValue ? Checkpoint.None : position.Value.ToCheckpoint();

	public static Checkpoint ToCheckpoint(this Position position) {
		Span<byte> checkpoint = stackalloc byte[8];

		BitConverter.TryWriteBytes(checkpoint, position.CommitPosition);

		return new Checkpoint(checkpoint.ToArray());
	}

	/// <summary>
	/// Full <c>$all</c> position (commit and prepare), so resuming lands exactly after the handled event.
	/// </summary>
	public static Checkpoint ToProcessManagerCheckpoint(this Position position) {
		Span<byte> checkpoint = stackalloc byte[16];

		BitConverter.TryWriteBytes(checkpoint[..8], position.CommitPosition);
		BitConverter.TryWriteBytes(checkpoint[8..], position.PreparePosition);

		return new Checkpoint(checkpoint.ToArray());
	}
}

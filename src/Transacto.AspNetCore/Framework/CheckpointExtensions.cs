using EventStore.Client;

namespace Transacto.Framework;

public static class CheckpointExtensions {
	public static FromAll ToEventStorePosition(this Checkpoint checkpoint) =>
		checkpoint == Checkpoint.None
			? FromAll.Start
			: FromAll.After(new Position(BitConverter.ToUInt64(checkpoint.Memory.Span[..8]),
				BitConverter.ToUInt64(checkpoint.Memory.Span[..8])));
}

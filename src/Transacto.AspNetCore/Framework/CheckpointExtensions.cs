using KurrentDB.Client;

namespace Transacto.Framework;

public static class CheckpointExtensions {
	public static FromAll ToEventStoreCheckpoint(this Checkpoint checkpoint) {
		if (checkpoint == Checkpoint.None) {
			return FromAll.Start;
		}

		var span = checkpoint.Memory.Span;
		var commitPosition = BitConverter.ToUInt64(span[..8]);
		// Process manager checkpoints also carry the prepare position. Older 8-byte checkpoints only have the commit.
		var preparePosition = span.Length >= 16 ? BitConverter.ToUInt64(span[8..16]) : commitPosition;

		return FromAll.After(new Position(commitPosition, preparePosition));
	}
}

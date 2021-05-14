using System;
using EventStore.Client;
using Transacto.Framework;

namespace Transacto {
	internal static class PositionExtensions {
		public static Checkpoint ToCheckpoint(this Position position) {
			if (position == Position.Start)
				return Checkpoint.None;

			Span<byte> checkpoint = stackalloc byte[16];

			BitConverter.TryWriteBytes(checkpoint, position.CommitPosition);
			BitConverter.TryWriteBytes(checkpoint[8..], position.PreparePosition);

			return new Checkpoint(checkpoint.ToArray());
		}
	}
}

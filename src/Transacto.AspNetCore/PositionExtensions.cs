using System;
using EventStore.Client;
using Transacto.Framework;

namespace Transacto {
	internal static class PositionExtensions {
		public static Checkpoint ToCheckpoint(this Position position) {
			if (position == Position.Start)
				return Checkpoint.None;

			var checkpoint = new byte[16];

			BitConverter.GetBytes(position.CommitPosition).CopyTo(checkpoint, 0);
			BitConverter.GetBytes(position.PreparePosition).CopyTo(checkpoint, 8);

			return new Checkpoint(checkpoint.AsMemory());
		}
	}
}

using System;
using EventStore.Client;
using Transacto.Framework;

namespace Transacto {
	internal static class CheckpointExtensions {
		public static Position ToEventStorePosition(this Checkpoint checkpoint) =>
			checkpoint == Checkpoint.None
				? Position.Start
				: new Position(BitConverter.ToUInt64(checkpoint.Memory.Slice(0, 8).Span),
					BitConverter.ToUInt64(checkpoint.Memory.Slice(8, 8).Span));
	}
}

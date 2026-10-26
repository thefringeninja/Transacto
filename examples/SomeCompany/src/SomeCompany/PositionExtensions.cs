using Transacto.Framework;

namespace SomeCompany;

internal static class PositionExtensions {
	public static Checkpoint ToCheckpoint(this Option<int> revision) => revision switch {
		Some<int> some => some.Value.ToCheckpoint(),
		None => Checkpoint.None,
	};

	public static Checkpoint ToCheckpoint(this int revision) {
		Span<byte> checkpoint = stackalloc byte[4];

		BitConverter.TryWriteBytes(checkpoint, revision);

		return new Checkpoint(checkpoint.ToArray());
	}
}

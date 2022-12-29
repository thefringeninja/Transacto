using Transacto.Framework;

namespace SomeCompany;

internal static class PositionExtensions {
	public static Checkpoint ToCheckpoint(this Optional<int> revision) =>
		!revision.HasValue ? Checkpoint.None : revision.Value.ToCheckpoint();

	public static Checkpoint ToCheckpoint(this int revision) {
		Span<byte> checkpoint = stackalloc byte[4];

		BitConverter.TryWriteBytes(checkpoint, revision);

		return new Checkpoint(checkpoint.ToArray());
	}
}

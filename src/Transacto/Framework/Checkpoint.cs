using System.Globalization;

namespace Transacto.Framework;

public readonly struct Checkpoint(ReadOnlyMemory<byte> value) : IEquatable<Checkpoint>, IComparable<Checkpoint> {
	public static readonly Checkpoint None = default;

	private readonly ReadOnlyMemory<byte> _value = value;
	public ReadOnlyMemory<byte> Memory => _value;
	public bool Equals(Checkpoint other) => CompareTo(other) == 0;
	public override bool Equals(object? obj) => obj is Checkpoint other && Equals(other);

	public override int GetHashCode() {
		// Bytes are little-endian, so trailing (most significant) zeros are ignored to match CompareTo.
		var span = _value.Span;
		var length = span.Length;
		while (length > 0 && span[length - 1] == 0) {
			length--;
		}

		var hash = new HashCode();
		hash.AddBytes(span[..length]);
		return hash.ToHashCode();
	}

	public static bool operator ==(Checkpoint left, Checkpoint right) => left.Equals(right);
	public static bool operator !=(Checkpoint left, Checkpoint right) => !left.Equals(right);
	public static bool operator <(Checkpoint left, Checkpoint right) => left.CompareTo(right) < 0;
	public static bool operator >(Checkpoint left, Checkpoint right) => left.CompareTo(right) > 0;
	public static bool operator <=(Checkpoint left, Checkpoint right) => left.CompareTo(right) <= 0;
	public static bool operator >=(Checkpoint left, Checkpoint right) => left.CompareTo(right) >= 0;

	public int CompareTo(Checkpoint other) {
		// Bytes are little-endian: compare the most significant byte (the last one) first.
		var left = _value.Span;
		var right = other._value.Span;
		var maxLength = Math.Max(left.Length, right.Length);
		for (var i = maxLength - 1; i >= 0; i--) {
			var l = i < left.Length ? left[i] : (byte)0;
			var r = i < right.Length ? right[i] : (byte)0;
			if (l != r) {
				return l.CompareTo(r);
			}
		}

		return 0;
	}

	public override string ToString() => Convert.ToHexString(_value.Span);

	public static Checkpoint FromString(ReadOnlySpan<char> value) {
		Span<byte> buf = stackalloc byte[value.Length / 2];
		for (var i = 0; i < buf.Length; i++) {
			buf[i] = byte.Parse(value.Slice(i * 2, 2), NumberStyles.HexNumber);
		}

		return new Checkpoint(buf.ToArray());
	}
}

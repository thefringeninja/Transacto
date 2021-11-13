using System;
using System.Globalization;

namespace Transacto.Framework; 

public readonly struct Checkpoint : IEquatable<Checkpoint> {
	private readonly ReadOnlyMemory<byte> _value;
	public ReadOnlyMemory<byte> Memory => _value;
	public Checkpoint(ReadOnlyMemory<byte> value) => _value = value;
	public bool Equals(Checkpoint other) => _value.Equals(other._value);
	public override bool Equals(object? obj) => obj is Checkpoint other && Equals(other);
	public override int GetHashCode() => _value.GetHashCode();
	public static bool operator ==(Checkpoint left, Checkpoint right) => left.Equals(right);
	public static bool operator !=(Checkpoint left, Checkpoint right) => !left.Equals(right);
	public static readonly Checkpoint None = default;
	public override string ToString() {
		Span<char> s = stackalloc char[_value.Length * 2];

		for (var i = 0; i < _value.Length; i++) {
			_value.Span[i].TryFormat(s[(i * 2)..], out _, "X");
		}

		return new string(s);
	}

	public static Checkpoint FromString(ReadOnlySpan<char> value) {
		var memory = new Memory<byte>(new byte[value.Length / 2]);
		for (var i = 0; i < memory.Length; i++) {
			memory.Span[i] = byte.Parse(value.Slice(i * 2, 2), NumberStyles.HexNumber);
		}

		return new Checkpoint(memory);
	}
}
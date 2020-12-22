using System;

namespace Transacto.Framework {
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
	}
}

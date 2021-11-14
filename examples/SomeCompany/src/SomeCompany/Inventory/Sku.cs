using System;

namespace SomeCompany.Inventory {
	public readonly struct Sku : IEquatable<Sku> {
		private readonly string _value;

		public Sku(string value) {
			if (value.Length == 0 || value.Length > 256) {
				throw new ArgumentException();
			}

			_value = value;
		}

		public bool Equals(Sku other) => _value == other._value;
		public override bool Equals(object? obj) => obj is Sku other && Equals(other);
		public override int GetHashCode() => _value != null ? _value.GetHashCode() : 0;
		public static bool operator ==(Sku left, Sku right) => left.Equals(right);
		public static bool operator !=(Sku left, Sku right) => !left.Equals(right);
		public override string ToString() => _value;
	}
}

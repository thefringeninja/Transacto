using System;

namespace Transacto.Domain {
	public readonly struct AccountNumber : IEquatable<AccountNumber> {
		public int Value { get; }

		public AccountNumber(int value) {
			if (value < 1000 || value >= 9000) {
				throw new ArgumentOutOfRangeException(nameof(value));
			}

			Value = value;
		}

		public bool Equals(AccountNumber other) => Value == other.Value;
		public override bool Equals(object? obj) => obj is AccountNumber other && Equals(other);
		public override int GetHashCode() => Value.GetHashCode();
		public static bool operator ==(AccountNumber left, AccountNumber right) => left.Equals(right);
		public static bool operator !=(AccountNumber left, AccountNumber right) => !left.Equals(right);
		public override string ToString() => Value.ToString();
		public int ToInt32() => Value;
	}
}

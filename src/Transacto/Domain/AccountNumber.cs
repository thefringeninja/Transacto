namespace Transacto.Domain;

public readonly struct AccountNumber : IEquatable<AccountNumber>, IComparable<AccountNumber> {
	public AccountNumber(int value) {
		ArgumentOutOfRangeException.ThrowIfLessThan(value, 1000);
		ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(value, 9000);
		Value = value;
	}

	public int Value { get; }

	public bool Equals(AccountNumber other) => Value == other.Value;
	public override bool Equals(object? obj) => obj is AccountNumber other && Equals(other);
	public override int GetHashCode() => Value.GetHashCode();
	public static bool operator ==(AccountNumber left, AccountNumber right) => left.Equals(right);
	public static bool operator !=(AccountNumber left, AccountNumber right) => !left.Equals(right);
	public override string ToString() => Value.ToString();
	public int ToInt32() => Value;
	public int CompareTo(AccountNumber other) => Value.CompareTo(other.Value);
}

namespace Transacto.Domain;

public readonly struct AccountNumber : IEquatable<AccountNumber> {
	public int Value { get; }

	public AccountNumber(int value) => Value = value switch {
		< 1000 or >= 9000 => throw new ArgumentOutOfRangeException(nameof(value)),
		_ => value
	};

	public bool Equals(AccountNumber other) => Value == other.Value;
	public override bool Equals(object? obj) => obj is AccountNumber other && Equals(other);
	public override int GetHashCode() => Value.GetHashCode();
	public static bool operator ==(AccountNumber left, AccountNumber right) => left.Equals(right);
	public static bool operator !=(AccountNumber left, AccountNumber right) => !left.Equals(right);
	public override string ToString() => Value.ToString();
	public int ToInt32() => Value;
}

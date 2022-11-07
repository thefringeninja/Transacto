namespace Transacto.Domain; 

public readonly struct AccountName : IEquatable<AccountName> {
	public const int MaxLength = 256;
	private readonly string _value;

	public AccountName(string value) => _value = value.Length switch {
		0 => throw new ArgumentException("Input was empty.", nameof(value)),
		> MaxLength => throw new ArgumentException("Input was too long.", nameof(value)),
		_ => value
	};

	public bool Equals(AccountName other) => _value == other._value;
	public override bool Equals(object? obj) => obj is AccountName other && Equals(other);
	public override int GetHashCode() => _value.GetHashCode();
	public static bool operator ==(AccountName left, AccountName right) => left.Equals(right);
	public static bool operator !=(AccountName left, AccountName right) => !left.Equals(right);
	public override string ToString() => _value;
}

namespace Transacto.Domain;

public readonly struct AccountNumberWidth : IEquatable<AccountNumberWidth> {
	private const int min = 4;
	private const int max = 8;

	public static readonly AccountNumberWidth Min = new(min);
	public static readonly AccountNumberWidth Max = new(max);

	private readonly int _value;

	public AccountNumberWidth(int value) {
		_value = value;
		ArgumentOutOfRangeException.ThrowIfLessThan(value, min);
		ArgumentOutOfRangeException.ThrowIfGreaterThan(value, max);
	}
	
	public bool Equals(AccountNumberWidth other) => _value == other._value;
	public override bool Equals(object? obj) => obj is AccountNumberWidth other && Equals(other);
	public override int GetHashCode() => _value;
	public static bool operator ==(AccountNumberWidth left, AccountNumberWidth right) => left.Equals(right);
	public static bool operator !=(AccountNumberWidth left, AccountNumberWidth right) => !left.Equals(right);
	public override string ToString() => _value.ToString();
	public int ToInt32() => _value;
}

namespace Transacto.Domain; 

public readonly struct Money : IEquatable<Money>, IComparable<Money> {
	private readonly decimal _value;

	public static readonly Money Zero = new(decimal.Zero);

	public Money(decimal value) => _value = value;

	public bool Equals(Money other) => _value == other._value;
	public override bool Equals(object? obj) => obj is Money other && Equals(other);
	public override int GetHashCode() => _value.GetHashCode();
	public decimal ToDecimal() => _value;
	public int CompareTo(Money other) => _value.CompareTo(other._value);
	public override string ToString() => _value.ToString();

	public static bool operator ==(Money left, Money right) => left.Equals(right);
	public static bool operator !=(Money left, Money right) => !left.Equals(right);
	public static bool operator <(Money left, Money right) => left._value < right._value;
	public static bool operator >(Money left, Money right) => left._value > right._value;
	public static bool operator <=(Money left, Money right) => left._value <= right._value;
	public static bool operator >=(Money left, Money right) => left._value >= right._value;
	public static Money operator +(Money left, Money right) => new(left._value + right._value);
	public static Money operator -(Money left, Money right) => new(left._value - right._value);
	public static Money operator +(Money left, decimal right) => new(left._value + right);
	public static Money operator -(Money left, decimal right) => new(left._value - right);
	public static Money operator -(Money value) => new(-value._value);
}

using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Numerics;

namespace Transacto.Domain;

public readonly struct Money(decimal value) : IEquatable<Money>,
	IComparable<Money>,
	IAdditiveIdentity<Money, Money>,
	IAdditionOperators<Money, Money, Money>,
	IAdditionOperators<Money, decimal, Money>,
	ISubtractionOperators<Money, Money, Money>,
	ISubtractionOperators<Money, decimal, Money>,
	IUnaryNegationOperators<Money, Money>,
	IMultiplyOperators<Money, Percentage, Money>,
	IMultiplyOperators<Money, decimal, Money> {
	private readonly decimal _value = value;

	public static readonly Money Zero = new(decimal.Zero);
	public static Money AdditiveIdentity { get; } = Zero;

	public bool Equals(Money other) => _value == other._value;
	public override bool Equals(object? obj) => obj is Money other && Equals(other);
	public override int GetHashCode() => _value.GetHashCode();
	public decimal ToDecimal() => _value;
	public int CompareTo(Money other) => _value.CompareTo(other._value);
	public override string ToString() => ToString("f");

	[Pure]
	public string ToString([StringSyntax("NumericFormat")] string? format) => _value.ToString(format);
	
	[Pure] public Money Round(int decimals) => new(Math.Round(_value, decimals, MidpointRounding.ToPositiveInfinity));

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
	public static Money operator *(Money left, Percentage right) => left * right.ToDecimal();
	public static Money operator *(Money left, decimal right) => new(left._value * right);
}

namespace Transacto.Domain;

public readonly struct Percentage(decimal value) : IEquatable<Percentage>,
	IComparable<Percentage> {
	private readonly decimal _value = value >= 0m ? value : throw new ArgumentOutOfRangeException(nameof(value));
	
	public static readonly Percentage Zero = new(0m);

	public decimal ToDecimal() => _value;
	public bool Equals(Percentage other) => _value == other._value;
	public override bool Equals(object? obj) => obj is Percentage other && Equals(other);
	public override int GetHashCode() => _value.GetHashCode();
	public int CompareTo(Percentage other) => _value.CompareTo(other._value);
	public static bool operator ==(Percentage left, Percentage right) => left.Equals(right);
	public static bool operator !=(Percentage left, Percentage right) => !left.Equals(right);
	public static bool operator <(Percentage left, Percentage right) => left.CompareTo(right) < 0;
	public static bool operator >(Percentage left, Percentage right) => left.CompareTo(right) > 0;
	public static bool operator <=(Percentage left, Percentage right) => left.CompareTo(right) <= 0;
	public static bool operator >=(Percentage left, Percentage right) => left.CompareTo(right) >= 0;
}

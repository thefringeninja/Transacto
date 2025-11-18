namespace Transacto.Domain;

public readonly struct Percentage(decimal value) : IEquatable<Percentage>,
	IComparable<Percentage> {
	private readonly decimal _value = value;
	public bool Equals(Percentage other) {
		throw new NotImplementedException();
	}

	public int CompareTo(Percentage other) {
		throw new NotImplementedException();
	}
}

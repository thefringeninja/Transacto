using System;

namespace Transacto.Domain; 

public readonly struct GeneralLedgerEntrySequenceNumber : IEquatable<GeneralLedgerEntrySequenceNumber> {
	private readonly int _value;

	public GeneralLedgerEntrySequenceNumber(int value) =>
		_value = value switch {
			<= 0 => throw new ArgumentOutOfRangeException(nameof(value)),
			_ => value
		};

	public bool Equals(GeneralLedgerEntrySequenceNumber other) => _value == other._value;
	public override bool Equals(object? obj) => obj is GeneralLedgerEntrySequenceNumber other && Equals(other);
	public override int GetHashCode() => _value;

	public static bool operator ==(GeneralLedgerEntrySequenceNumber left, GeneralLedgerEntrySequenceNumber right) =>
		left.Equals(right);

	public static bool operator !=(GeneralLedgerEntrySequenceNumber left, GeneralLedgerEntrySequenceNumber right) =>
		!left.Equals(right);

	public override string ToString() => _value.ToString();
	public int ToInt32() => _value;
}
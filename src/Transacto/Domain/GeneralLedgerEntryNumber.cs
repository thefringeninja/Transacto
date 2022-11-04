using System;

namespace Transacto.Domain; 

public readonly struct GeneralLedgerEntryNumber : IEquatable<GeneralLedgerEntryNumber> {
	public GeneralLedgerEntryNumberPrefix Prefix { get; }
	public GeneralLedgerEntrySequenceNumber SequenceNumber { get; }

	public GeneralLedgerEntryNumber(GeneralLedgerEntryNumberPrefix prefix,
		GeneralLedgerEntrySequenceNumber sequenceNumber) {
		Prefix = prefix;
		SequenceNumber = sequenceNumber;
	}

	public static GeneralLedgerEntryNumber Parse(string value) =>
		TryParse(value, out var result)
			? result
			: throw new FormatException();

	public static bool TryParse(string value, out GeneralLedgerEntryNumber generalLedgerEntryNumber) {
		generalLedgerEntryNumber = default;
		var indexOfDelimiter = value.IndexOf('-');
		if (indexOfDelimiter < 0 || indexOfDelimiter != value.LastIndexOf('-')) {
			return false;
		}

		var prefix = value[..indexOfDelimiter];
		if (string.IsNullOrWhiteSpace(prefix)) {
			return false;
		}

		if (!int.TryParse(value[(indexOfDelimiter + 1)..], out var sequenceNumber)) {
			return false;
		}

		generalLedgerEntryNumber = new GeneralLedgerEntryNumber(new(prefix), new(sequenceNumber));
		return true;
	}

	public bool Equals(GeneralLedgerEntryNumber other) =>
		Prefix == other.Prefix && SequenceNumber == other.SequenceNumber;

	public override bool Equals(object? obj) => obj is GeneralLedgerEntryNumber other && Equals(other);
	public override int GetHashCode() => HashCode.Combine(Prefix, SequenceNumber);
	public override string ToString() => $"{Prefix}-{SequenceNumber}";

	public static bool operator ==(GeneralLedgerEntryNumber left, GeneralLedgerEntryNumber right) =>
		left.Equals(right);

	public static bool operator !=(GeneralLedgerEntryNumber left, GeneralLedgerEntryNumber right) =>
		!left.Equals(right);
}
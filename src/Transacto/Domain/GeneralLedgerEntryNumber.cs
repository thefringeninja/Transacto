using System;
using System.Linq;

namespace Transacto.Domain {
	public readonly struct GeneralLedgerEntryNumber : IEquatable<GeneralLedgerEntryNumber> {
		public const int MaxPrefixLength = 5;
		public string Prefix { get; }
		public int SequenceNumber { get; }

		public GeneralLedgerEntryNumber(string prefix, int sequenceNumber) {
			if (prefix == string.Empty) {
				throw new ArgumentException("Prefix may not be empty.", nameof(prefix));
			}

			if (prefix.Length > MaxPrefixLength) {
				throw new ArgumentException($"Prefix may not exceed {MaxPrefixLength} characters.", nameof(prefix));
			}

			if (prefix.Any(char.IsWhiteSpace)) {
				throw new ArgumentException("Prefix may not contain whitespace.", nameof(prefix));
			}

			if (sequenceNumber <= 0) {
				throw new ArgumentOutOfRangeException(nameof(sequenceNumber));
			}

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
			if (indexOfDelimiter < 1 || indexOfDelimiter != value.LastIndexOf('-')) {
				return false;
			}

			var prefix = value[..indexOfDelimiter];
			if (string.IsNullOrWhiteSpace(prefix)) {
				return false;
			}

			if (!int.TryParse(value[(indexOfDelimiter + 1)..], out var sequenceNumber)) {
				return false;
			}

			generalLedgerEntryNumber = new GeneralLedgerEntryNumber(prefix, sequenceNumber);
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
}

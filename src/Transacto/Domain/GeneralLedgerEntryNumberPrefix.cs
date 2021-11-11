using System;
using System.Linq;

namespace Transacto.Domain {
	public readonly struct GeneralLedgerEntryNumberPrefix : IEquatable<GeneralLedgerEntryNumberPrefix> {
		public const int MaxPrefixLength = 255;

		public static GeneralLedgerEntryNumberPrefix JournalEntryNumberPrefix = new("je");

		private readonly string _value;

		public GeneralLedgerEntryNumberPrefix(string value) {
			_value = value switch {
				null => throw new ArgumentNullException(nameof(value)),
				"" => throw new ArgumentException("Prefix may not be empty.",
					nameof(value)),
				{ Length: > MaxPrefixLength } => throw new ArgumentException(
					$"Prefix may not exceed {MaxPrefixLength} characters.", nameof(value)),
				{ } when value.Any(char.IsWhiteSpace) => throw new ArgumentException(
					"Prefix may not contain whitespace.", nameof(value)),
				_ => value
			};
		}

		public bool Equals(GeneralLedgerEntryNumberPrefix other) => _value == other._value;
		public override bool Equals(object? obj) => obj is GeneralLedgerEntryNumberPrefix other && Equals(other);
		public override int GetHashCode() => _value.GetHashCode();

		public static bool operator ==(GeneralLedgerEntryNumberPrefix left, GeneralLedgerEntryNumberPrefix right) =>
			left.Equals(right);

		public static bool operator !=(GeneralLedgerEntryNumberPrefix left, GeneralLedgerEntryNumberPrefix right) =>
			!left.Equals(right);

		public override string ToString() => _value;
	}
}

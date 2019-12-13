using System;

namespace Transacto.Domain {
    public readonly struct GeneralLedgerEntryNumber : IEquatable<GeneralLedgerEntryNumber> {
        private readonly string _value;

        public GeneralLedgerEntryNumber(string value) {
            if (!value.Contains("-")) {
                throw new ArgumentException();
            }

            _value = value;
        }

        public bool Equals(GeneralLedgerEntryNumber other) => _value == other._value;
        public override bool Equals(object? obj) => obj is GeneralLedgerEntryNumber other && Equals(other);
        public override int GetHashCode() => _value.GetHashCode();
        public override string ToString() => _value;

        public static bool operator ==(GeneralLedgerEntryNumber left, GeneralLedgerEntryNumber right) =>
            left.Equals(right);

        public static bool operator !=(GeneralLedgerEntryNumber left, GeneralLedgerEntryNumber right) =>
            !left.Equals(right);
    }
}

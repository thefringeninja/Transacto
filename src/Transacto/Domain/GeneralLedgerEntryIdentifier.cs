using System;

namespace Transacto.Domain {
    public struct GeneralLedgerEntryIdentifier : IEquatable<GeneralLedgerEntryIdentifier> {
        private readonly Guid _value;

        public GeneralLedgerEntryIdentifier(Guid value) {
            if (value == Guid.Empty) {
                throw new InvalidOperationException();
            }

            _value = value;
        }

        public bool Equals(GeneralLedgerEntryIdentifier other) => _value.Equals(other._value);
        public override bool Equals(object obj) => obj is GeneralLedgerEntryIdentifier other && Equals(other);
        public override int GetHashCode() => _value.GetHashCode();

        public static bool operator ==(GeneralLedgerEntryIdentifier left, GeneralLedgerEntryIdentifier right) =>
            left.Equals(right);

        public static bool operator !=(GeneralLedgerEntryIdentifier left, GeneralLedgerEntryIdentifier right) =>
            !left.Equals(right);

        public Guid ToGuid() => _value;
    }
}

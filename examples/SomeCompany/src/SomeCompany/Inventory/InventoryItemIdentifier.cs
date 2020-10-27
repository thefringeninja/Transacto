using System;

#nullable enable
namespace SomeCompany.Inventory {
	public readonly struct InventoryItemIdentifier : IEquatable<InventoryItemIdentifier> {
		private readonly Guid _value;

		public InventoryItemIdentifier(Guid value) {
			if (value == Guid.Empty) {
				throw new ArgumentOutOfRangeException(nameof(value));
			}

			_value = value;
		}

		public bool Equals(InventoryItemIdentifier other) => _value.Equals(other._value);
		public override bool Equals(object? obj) => obj is InventoryItemIdentifier other && Equals(other);
		public override int GetHashCode() => _value.GetHashCode();

		public static bool operator ==(InventoryItemIdentifier left, InventoryItemIdentifier right) =>
			left.Equals(right);

		public static bool operator !=(InventoryItemIdentifier left, InventoryItemIdentifier right) =>
			!left.Equals(right);

		public Guid ToGuid() => _value;
		public override string ToString() => _value.ToString("n");
	}
}

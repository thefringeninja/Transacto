namespace Transacto.Domain;

public readonly struct GeneralLedgerEntryNumberPrefix : IEquatable<GeneralLedgerEntryNumberPrefix> {
	public const int MaxPrefixLength = 255;

	private readonly string _value;

	public GeneralLedgerEntryNumberPrefix(string value) {
		ArgumentNullException.ThrowIfNull(value);
		ArgumentException.ThrowIfNullOrEmpty(value);
		ArgumentOutOfRangeException.ThrowIfGreaterThan(value.Length, MaxPrefixLength);
		if (value.Any(char.IsWhiteSpace))
			throw new ArgumentException("Prefix may not contain whitespace.", nameof(value));
		_value = value;
	}

	public bool Equals(GeneralLedgerEntryNumberPrefix other) => _value == other._value;
	public override bool Equals(object? obj) => obj is GeneralLedgerEntryNumberPrefix other && Equals(other);
	public override int GetHashCode() => _value?.GetHashCode() ?? 0;

	public static bool operator ==(GeneralLedgerEntryNumberPrefix left, GeneralLedgerEntryNumberPrefix right) =>
		left.Equals(right);

	public static bool operator !=(GeneralLedgerEntryNumberPrefix left, GeneralLedgerEntryNumberPrefix right) =>
		!left.Equals(right);

	public override string ToString() => _value;
}

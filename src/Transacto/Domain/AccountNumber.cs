namespace Transacto.Domain;

public readonly struct AccountNumber : IEquatable<AccountNumber>, IComparable<AccountNumber> {
	public AccountNumber(int value) {
		ArgumentOutOfRangeException.ThrowIfNegative(value);
		ArgumentOutOfRangeException.ThrowIfZero(value);
		var accountType = GetAccountType(value);
		ArgumentOutOfRangeException.ThrowIfLessThan(accountType, 1, nameof(value));
		ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(accountType, 9, nameof(value));
		Value = value;
	}

	public int Value { get; }

	public bool Equals(AccountNumber other) => Value == other.Value;
	public override bool Equals(object? obj) => obj is AccountNumber other && Equals(other);
	public override int GetHashCode() => Value.GetHashCode();
	public static bool operator ==(AccountNumber left, AccountNumber right) => left.Equals(right);
	public static bool operator !=(AccountNumber left, AccountNumber right) => !left.Equals(right);
	public override string ToString() => Value.ToString();
	public int ToInt32() => Value;
	public int CompareTo(AccountNumber other) => Value.CompareTo(other.Value);

	private static int GetAccountType(int accountNumber) {
		accountNumber = Math.Abs(accountNumber);

		if (accountNumber == 0) {
			return 0;
		}

		while (accountNumber >= 10) {
			accountNumber /= 10;
		}

		return accountNumber;
	}
}

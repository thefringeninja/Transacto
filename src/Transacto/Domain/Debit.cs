namespace Transacto.Domain;

public readonly struct Debit : IEquatable<Debit> {
	public Money Amount { get; }
	public AccountNumber AccountNumber { get; }

	public Debit(AccountNumber accountNumber) : this(accountNumber, Money.Zero) {
	}

	public Debit(AccountNumber accountNumber, Money amount) {
		if (amount < Money.Zero) {
			throw new ArgumentOutOfRangeException(nameof(amount));
		}

		Amount = amount;
		AccountNumber = accountNumber;
	}

	public override int GetHashCode() => unchecked((Amount.GetHashCode() * 397) ^ AccountNumber.GetHashCode());
	public bool Equals(Debit other) => Amount.Equals(other.Amount) && AccountNumber.Equals(other.AccountNumber);
	public override bool Equals(object? obj) => obj is Debit other && Equals(other);
	public static bool operator ==(Debit left, Debit right) => left.Equals(right);
	public static bool operator !=(Debit left, Debit right) => !left.Equals(right);
	public static Debit operator +(Debit left, Money right) => new(left.AccountNumber, left.Amount + right);
	public static Debit operator -(Debit left, Money right) => new(left.AccountNumber, left.Amount - right);
	public static Debit operator +(Debit left, decimal right) => new(left.AccountNumber, left.Amount + right);
	public static Debit operator -(Debit left, decimal right) => new(left.AccountNumber, left.Amount - right);
}

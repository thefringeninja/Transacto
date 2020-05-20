using System;

namespace Transacto.Domain {
	public readonly struct Debit {
		public Money Amount { get; }
		public AccountNumber AccountNumber { get; }

		private readonly AccountType _accountType;

		public Debit(AccountNumber accountNumber) : this(accountNumber, Money.Zero) {
		}

		public Debit(AccountNumber accountNumber, Money amount) {
			if (amount < Money.Zero) {
				throw new ArgumentOutOfRangeException(nameof(amount));
			}

			Amount = amount;
			AccountNumber = accountNumber;
			_accountType = AccountType.OfAccountNumber(accountNumber);
		}

		public bool AppearsOnBalanceSheet => _accountType.AppearsOnBalanceSheet;
		public bool AppearsOnProfitAndLoss => _accountType.AppearsOnProfitAndLoss;
		public override int GetHashCode() => HashCode.Combine(Amount, AccountNumber);
		public bool Equals(Debit other) => Amount.Equals(other.Amount) && AccountNumber.Equals(other.AccountNumber);
		public override bool Equals(object? obj) => obj is Debit other && Equals(other);
		public static bool operator ==(Debit left, Debit right) => left.Equals(right);
		public static bool operator !=(Debit left, Debit right) => !left.Equals(right);
		public static Debit operator +(Debit left, Money right) => new Debit(left.AccountNumber, left.Amount + right);
		public static Debit operator -(Debit left, Money right) => new Debit(left.AccountNumber, left.Amount - right);
		public static Debit operator +(Debit left, decimal right) => new Debit(left.AccountNumber, left.Amount + right);
		public static Debit operator -(Debit left, decimal right) => new Debit(left.AccountNumber, left.Amount - right);
	}
}

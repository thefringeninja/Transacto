using System;

namespace Transacto.Domain {
	public readonly struct Credit : IEquatable<Credit> {
		public AccountNumber AccountNumber { get; }
		public Money Amount { get; }

		private readonly AccountType _accountType;

		public Credit(AccountNumber accountNumber) : this(accountNumber, Money.Zero) {
		}

		public Credit(AccountNumber accountNumber, Money amount) {
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
		public bool Equals(Credit other) => Amount.Equals(other.Amount) && AccountNumber.Equals(other.AccountNumber);
		public override bool Equals(object? obj) => obj is Credit other && Equals(other);
		public static bool operator ==(Credit left, Credit right) => left.Equals(right);
		public static bool operator !=(Credit left, Credit right) => !left.Equals(right);

		public static Credit operator +(Credit left, Money right) =>
			new Credit(left.AccountNumber, left.Amount + right);

		public static Credit operator -(Credit left, Money right) =>
			new Credit(left.AccountNumber, left.Amount - right);

		public static Credit operator +(Credit left, decimal right) =>
			new Credit(left.AccountNumber, left.Amount + right);

		public static Credit operator -(Credit left, decimal right) =>
			new Credit(left.AccountNumber, left.Amount - right);
	}
}

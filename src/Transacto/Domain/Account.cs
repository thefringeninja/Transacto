using System;
using System.Linq;

namespace Transacto.Domain {
	public abstract record Account {
		public AccountName AccountName { get; }
		public AccountNumber AccountNumber { get; }
		public Money Balance { get; protected init; }

		public static Account For(AccountNumber accountNumber, AccountName accountName = default) =>
			accountNumber.ToInt32() switch {
				>= 1000 and < 2000 => new AssetAccount(accountName, accountNumber),
				>= 2000 and < 3000 => new LiabilityAccount(accountName, accountNumber),
				>= 3000 and < 4000 => new EquityAccount(accountName, accountNumber),
				>= 4000 and < 5000 => new IncomeAccount(accountName, accountNumber),
				>= 5000 and < 6000 => new ExpenseAccount(accountName, accountNumber),
				>= 6000 and < 7000 => new ExpenseAccount(accountName, accountNumber),
				>= 7000 and < 8000 => new IncomeAccount(accountName, accountNumber),
				>= 8000 and < 9000 => new ExpenseAccount(accountName, accountNumber),
				_ => throw new ArgumentOutOfRangeException(nameof(accountNumber))
			};

		// ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
		protected Account(AccountName accountName, AccountNumber accountNumber,
			params Range[] accountNumberRanges) {
			if (accountNumberRanges.Length == 0) {
				throw new ArgumentException("No valid account number range was specified.",
					nameof(accountNumberRanges));
			}

			if (!accountNumberRanges.Any(range => accountNumber.Value >= range.Start.Value &&
			                                      accountNumber.Value < range.End.Value)) {
				throw new ArgumentOutOfRangeException(nameof(accountNumber));
			}

			AccountName = accountName;
			AccountNumber = accountNumber;
		}

		public abstract Account Credit(Money amount);
		public abstract Account Debit(Money amount);
	}
}

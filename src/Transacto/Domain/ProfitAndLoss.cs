using System;
using System.Collections.Generic;
using System.Linq;
using NodaTime;

namespace Transacto.Domain {
	public class ProfitAndLoss {
		private static bool IgnoreInactiveAccount(AccountNumber _) => false;

		private readonly Period _period;
		private readonly IDictionary<AccountNumber, Money> _income;
		private readonly IDictionary<AccountNumber, Money> _expenses;

		public ProfitAndLoss(Period period) {
			_period = period;
			_income = new Dictionary<AccountNumber, Money>();
			_expenses = new Dictionary<AccountNumber, Money>();
		}

		public GeneralLedgerEntry GetClosingEntry(AccountIsDeactivated accountIsDeactivated,
			AccountNumber retainedEarningsAccountNumber, LocalDateTime closedOn,
			GeneralLedgerEntryIdentifier closingGeneralLedgerEntryIdentifier) {
			var entry = new GeneralLedgerEntry(closingGeneralLedgerEntryIdentifier,
				new GeneralLedgerEntryNumber("jec", int.Parse(_period.ToString())), _period, closedOn);
			foreach (var (accountNumber, amount) in _income) {
				if (amount == Money.Zero) {
					continue;
				}

				if (amount > Money.Zero) {
					entry.ApplyCredit(new Credit(accountNumber, amount), IgnoreInactiveAccount);
				} else {
					entry.ApplyDebit(new Debit(accountNumber, -amount), IgnoreInactiveAccount);
				}
			}

			foreach (var (accountNumber, amount) in _expenses) {
				if (amount == Money.Zero) {
					continue;
				}

				if (amount < Money.Zero) {
					entry.ApplyCredit(new Credit(accountNumber, amount), IgnoreInactiveAccount);
				} else {
					entry.ApplyDebit(new Debit(accountNumber, -amount), IgnoreInactiveAccount);
				}
			}

			var retainedEarnings = entry.Debits.Select(x => x.Amount).Sum() -
			                       entry.Credits.Select(x => x.Amount).Sum();

			if (retainedEarnings < Money.Zero) {
				entry.ApplyDebit(new Debit(retainedEarningsAccountNumber, -retainedEarnings), accountIsDeactivated);
			} else if (retainedEarnings > Money.Zero) {
				entry.ApplyCredit(new Credit(retainedEarningsAccountNumber, retainedEarnings),
					accountIsDeactivated);
			}

			entry.Post();

			return entry;
		}

		public void Transfer(GeneralLedgerEntry generalLedgerEntry) {
			foreach (var credit in generalLedgerEntry.Credits) {
				var accountType = AccountType.OfAccountNumber(credit.AccountNumber);
				switch (accountType) {
					case AccountType.ExpenseAccount: {
						_expenses[credit.AccountNumber] =
							_expenses.TryGetValue(credit.AccountNumber, out var amount)
								? amount + credit.Amount
								: credit.Amount;
						break;
					}
					case AccountType.IncomeAccount: {
						_income[credit.AccountNumber] = _income.TryGetValue(credit.AccountNumber, out var amount)
							? amount - credit.Amount
							: -credit.Amount;
						break;
					}
				}
			}

			foreach (var debit in generalLedgerEntry.Debits) {
				var accountType = AccountType.OfAccountNumber(debit.AccountNumber);
				switch (accountType) {
					case AccountType.ExpenseAccount: {
						_expenses[debit.AccountNumber] = _expenses.TryGetValue(debit.AccountNumber, out var amount)
							? amount - debit.Amount
							: -debit.Amount;
						break;
					}
					case AccountType.IncomeAccount: {
						_income[debit.AccountNumber] = _income.TryGetValue(debit.AccountNumber, out var amount)
							? amount + debit.Amount
							: debit.Amount;
						break;
					}
				}
			}
		}
	}
}

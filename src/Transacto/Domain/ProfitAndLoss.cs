using NodaTime;

namespace Transacto.Domain;

public class ProfitAndLoss(AccountingPeriod accountingPeriod, ChartOfAccounts chartOfAccounts) {
	private static bool IgnoreInactiveAccount(AccountNumber _) => false;

	private readonly AccountBalanceCollection _current = new();

	public GeneralLedgerEntry GetClosingEntry(AccountIsDeactivated accountIsDeactivated,
		EquityAccount retainedEarningsAccount, LocalDateTime closedOn,
		GeneralLedgerEntryIdentifier closingGeneralLedgerEntryIdentifier) {
		var entry = new GeneralLedgerEntry(closingGeneralLedgerEntryIdentifier,
			new GeneralLedgerEntryNumber(new("jec"), new(int.Parse(accountingPeriod.ToString()))),
			accountingPeriod,
			closedOn);

		foreach (var account in _current.Where(x => x.Balance != Money.Zero)) {
			switch (account) {
				case IncomeAccount: {
					if (account.Balance > Money.Zero) {
						entry.ApplyDebit(new(account.AccountNumber, account.Balance), IgnoreInactiveAccount);
					} else {
						entry.ApplyCredit(new(account.AccountNumber, -account.Balance), IgnoreInactiveAccount);
					}

					continue;
				}
				case ExpenseAccount:
					if (account.Balance > Money.Zero) {
						entry.ApplyCredit(new(account.AccountNumber, account.Balance), IgnoreInactiveAccount);
					} else {
						entry.ApplyDebit(new(account.AccountNumber, -account.Balance), IgnoreInactiveAccount);
					}

					continue;
				default:
					continue;
			}
		}

		var retainedEarnings = entry.Debits.Sum() - entry.Credits.Sum();

		if (retainedEarnings < Money.Zero) {
			entry.ApplyDebit(new(retainedEarningsAccount.AccountNumber, -retainedEarnings), accountIsDeactivated);
		} else if (retainedEarnings > Money.Zero) {
			entry.ApplyCredit(new(retainedEarningsAccount.AccountNumber, retainedEarnings), accountIsDeactivated);
		}

		entry.Post();

		return entry;
	}

	public void Transfer(GeneralLedgerEntry generalLedgerEntry) {
		foreach (var credit in generalLedgerEntry.Credits) {
			_current.AddOrUpdate(credit.AccountNumber, () => chartOfAccounts[credit.AccountNumber],
				account => account.Credit(credit.Amount));
		}

		foreach (var debit in generalLedgerEntry.Debits) {
			_current.AddOrUpdate(debit.AccountNumber, () => chartOfAccounts[debit.AccountNumber],
				account => account.Debit(debit.Amount));
		}
	}
}

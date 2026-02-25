using NodaTime;

namespace Transacto.Domain;

public class ProfitAndLoss(AccountingPeriod accountingPeriod, ChartOfAccounts chartOfAccounts) {
	private readonly AccountBalanceCollection _current = new();

	public GeneralLedgerEntry GetClosingEntry(EquityAccount retainedEarningsAccount, LocalDate closedOn,
		GeneralLedgerEntryIdentifier closingGeneralLedgerEntryIdentifier) {
		var entry = new GeneralLedgerEntry(closingGeneralLedgerEntryIdentifier,
			accountingPeriod,
			closedOn);

		foreach (var account in _current.Where(x => x.Balance != Money.Zero)) {
			switch (account) {
				case IncomeAccount: {
					if (account.Balance > Money.Zero) {
						entry.ApplyDebit(new(account.AccountNumber, account.Balance), AccountIsDeactivated.Ignore);
					} else {
						entry.ApplyCredit(new(account.AccountNumber, -account.Balance), AccountIsDeactivated.Ignore);
					}

					continue;
				}
				case ExpenseAccount:
					if (account.Balance > Money.Zero) {
						entry.ApplyCredit(new(account.AccountNumber, account.Balance), AccountIsDeactivated.Ignore);
					} else {
						entry.ApplyDebit(new(account.AccountNumber, -account.Balance), AccountIsDeactivated.Ignore);
					}

					continue;
				default:
					continue;
			}
		}

		var retainedEarnings = entry.Debits.Sum() - entry.Credits.Sum();

		if (retainedEarnings < Money.Zero) {
			entry.ApplyDebit(new(retainedEarningsAccount.AccountNumber, -retainedEarnings), AccountIsDeactivated.Ignore);
		} else if (retainedEarnings > Money.Zero) {
			entry.ApplyCredit(new(retainedEarningsAccount.AccountNumber, retainedEarnings), AccountIsDeactivated.Ignore);
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

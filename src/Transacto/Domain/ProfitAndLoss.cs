using System.Linq;
using NodaTime;

namespace Transacto.Domain; 

public class ProfitAndLoss {
	private static bool IgnoreInactiveAccount(AccountNumber _) => false;

	private readonly AccountingPeriod _accountingPeriod;
	private readonly ChartOfAccounts _chartOfAccounts;
	private readonly AccountCollection _current;

	public ProfitAndLoss(AccountingPeriod accountingPeriod, ChartOfAccounts chartOfAccounts) {
		_accountingPeriod = accountingPeriod;
		_chartOfAccounts = chartOfAccounts;
		_current = new AccountCollection();
	}

	public GeneralLedgerEntry GetClosingEntry(AccountIsDeactivated accountIsDeactivated,
		EquityAccount retainedEarningsAccount, LocalDateTime closedOn,
		GeneralLedgerEntryIdentifier closingGeneralLedgerEntryIdentifier) {
		var entry = new GeneralLedgerEntry(closingGeneralLedgerEntryIdentifier,
			new GeneralLedgerEntryNumber(new("jec"), new(int.Parse(_accountingPeriod.ToString()))),
			_accountingPeriod,
			closedOn);

		foreach (var account in _current.Where(x => x.Balance != Money.Zero)
			         .OrderBy(x => x.AccountNumber.ToInt32())) {
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

		var retainedEarnings = entry.Debits.Select(x => x.Amount).Sum() -
		                       entry.Credits.Select(x => x.Amount).Sum();

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
			_current.AddOrUpdate(credit.AccountNumber, () => _chartOfAccounts[credit.AccountNumber],
				account => account.Credit(credit.Amount));
		}

		foreach (var debit in generalLedgerEntry.Debits) {
			_current.AddOrUpdate(debit.AccountNumber, () => _chartOfAccounts[debit.AccountNumber],
				account => account.Debit(debit.Amount));
		}
	}
}
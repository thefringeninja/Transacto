using System.Collections;

namespace Transacto.Domain; 

public class TrialBalance : IEnumerable<Account> {
	private readonly ChartOfAccounts _chartOfAccounts;
	private readonly AccountCollection _current;

	public TrialBalance(ChartOfAccounts chartOfAccounts) {
		_chartOfAccounts = chartOfAccounts;
		_current = new AccountCollection();
	}

	public void Transfer(GeneralLedgerEntry generalLedgerEntry) {
		foreach (var debit in generalLedgerEntry.Debits) {
			_current.AddOrUpdate(debit.AccountNumber, () => _chartOfAccounts[debit.AccountNumber],
				account => account.Debit(debit.Amount));
		}

		foreach (var credit in generalLedgerEntry.Credits) {
			_current.AddOrUpdate(credit.AccountNumber, () => _chartOfAccounts[credit.AccountNumber],
				account => account.Credit(credit.Amount));
		}
	}

	public void MustBeInBalance() {
		var balance = _current.Select(account => account switch {
			ExpenseAccount or AssetAccount => account.Balance,
			_ => -account.Balance
		}).Sum();
		if (balance != Money.Zero) {
			throw new TrialBalanceFailedException(balance);
		}
	}

	public IEnumerator<Account> GetEnumerator() => _current.OrderBy(x => x.AccountNumber.ToInt32()).GetEnumerator();
	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

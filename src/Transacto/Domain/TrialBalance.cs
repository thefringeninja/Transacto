using System.Collections;

namespace Transacto.Domain;

public class TrialBalance(ChartOfAccounts chartOfAccounts) : IEnumerable<Account> {
	private readonly AccountBalanceCollection _current = new();

	public void Transfer(GeneralLedgerEntry generalLedgerEntry) {
		foreach (var debit in generalLedgerEntry.Debits) {
			_current.AddOrUpdate(debit.AccountNumber, () => chartOfAccounts[debit.AccountNumber],
				account => account.Debit(debit.Amount));
		}

		foreach (var credit in generalLedgerEntry.Credits) {
			_current.AddOrUpdate(credit.AccountNumber, () => chartOfAccounts[credit.AccountNumber],
				account => account.Credit(credit.Amount));
		}
	}

	public void MustBeInBalance() {
		var balance = _current.GetBalance();

		if (balance != Money.Zero) {
			throw new TrialBalanceFailedException(balance);
		}
	}

	public IEnumerator<Account> GetEnumerator() => _current.OrderBy(x => x.AccountNumber).GetEnumerator();
	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

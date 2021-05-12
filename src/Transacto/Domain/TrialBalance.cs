using System.Collections;
using System.Collections.Generic;

namespace Transacto.Domain {
	public class TrialBalance : IEnumerable<KeyValuePair<AccountNumber, Money>> {
		public static TrialBalance None => new();

		private readonly IDictionary<AccountNumber, Money> _inner;

		private TrialBalance() {
			_inner = new Dictionary<AccountNumber, Money>();
		}

		public void Transfer(GeneralLedgerEntry generalLedgerEntry) {
			foreach (var debit in generalLedgerEntry.Debits) {
				_inner[debit.AccountNumber] = _inner.TryGetValue(debit.AccountNumber, out var amount)
					? amount + debit.Amount
					: debit.Amount;
			}

			foreach (var credit in generalLedgerEntry.Credits) {
				_inner[credit.AccountNumber] = _inner.TryGetValue(credit.AccountNumber, out var amount)
					? amount - credit.Amount
					: -credit.Amount;
			}
		}

		public void Apply(AccountNumber accountNumber, Money amount) => _inner[accountNumber] = amount;

		public void MustBeInBalance() {
			var balance = _inner.Values.Sum();
			if (balance != Money.Zero) {
				throw new TrialBalanceFailedException(balance);
			}
		}

		public IEnumerator<KeyValuePair<AccountNumber, Money>> GetEnumerator() => _inner.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_inner).GetEnumerator();
	}
}

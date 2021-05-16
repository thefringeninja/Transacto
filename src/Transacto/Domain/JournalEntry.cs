using System;
using System.Collections.Generic;

namespace Transacto.Domain {
	public class JournalEntry : IBusinessTransaction {
		GeneralLedgerEntryNumber IBusinessTransaction.ReferenceNumber => new("je", ReferenceNumber);

		public int ReferenceNumber { get; set; }
		public Item[] Credits { get; set; } = Array.Empty<Item>();
		public Item[] Debits { get; set; } = Array.Empty<Item>();

		public void Apply(GeneralLedgerEntry generalLedgerEntry, AccountIsDeactivated accountIsDeactivated) {
			foreach (var credit in Credits) {
				generalLedgerEntry.ApplyCredit(
					new Credit(new AccountNumber(credit.AccountNumber), new Money(credit.Amount)),
					accountIsDeactivated);
			}

			foreach (var debit in Debits) {
				generalLedgerEntry.ApplyDebit(
					new Debit(new AccountNumber(debit.AccountNumber), new Money(debit.Amount)),
					accountIsDeactivated);
			}
		}

		public IEnumerable<object> GetAdditionalChanges() {
			yield break;
		}

		public int? Version { get; set; }

		public class Item {
			public int AccountNumber { get; set; }
			public decimal Amount { get; set; }
		}
	}
}

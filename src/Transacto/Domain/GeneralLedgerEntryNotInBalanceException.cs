using System;

namespace Transacto.Domain {
	public class GeneralLedgerEntryNotInBalanceException : Exception {
		public GeneralLedgerEntryIdentifier GeneralLedgerEntryIdentifier { get; }

		public GeneralLedgerEntryNotInBalanceException(GeneralLedgerEntryIdentifier generalLedgerEntryIdentifier)
			: base("The general ledger entry was not in balance.") {
			GeneralLedgerEntryIdentifier = generalLedgerEntryIdentifier;
		}
	}
}

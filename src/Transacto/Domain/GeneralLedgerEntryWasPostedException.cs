using System;

namespace Transacto.Domain {
	public class GeneralLedgerEntryWasPostedException : Exception {
		public GeneralLedgerEntryIdentifier GeneralLedgerEntryIdentifier { get; }

		public GeneralLedgerEntryWasPostedException(GeneralLedgerEntryIdentifier generalLedgerEntryIdentifier)
			: base("The general ledger entry was posted.") {
			GeneralLedgerEntryIdentifier = generalLedgerEntryIdentifier;
		}
	}
}

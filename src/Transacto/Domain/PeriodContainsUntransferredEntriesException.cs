using System;

namespace Transacto.Domain {
	public class PeriodContainsUntransferredEntriesException : Exception {
		public AccountingPeriod AccountingPeriod { get; }
		public GeneralLedgerEntryIdentifier[] UntransferredGeneralLedgerEntryIdentifiers { get; }

		public PeriodContainsUntransferredEntriesException(AccountingPeriod accountingPeriod,
			GeneralLedgerEntryIdentifier[] untransferredGeneralLedgerEntryIdentifiers) : base(
			$"Period {accountingPeriod} contains un-transferred entries: {string.Join(", ", untransferredGeneralLedgerEntryIdentifiers)}") {
			AccountingPeriod = accountingPeriod;
			UntransferredGeneralLedgerEntryIdentifiers = untransferredGeneralLedgerEntryIdentifiers;
		}
	}
}

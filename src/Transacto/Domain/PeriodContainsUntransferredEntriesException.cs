using System;

namespace Transacto.Domain {
	public class PeriodContainsUntransferredEntriesException : Exception {
		public Period Period { get; }
		public GeneralLedgerEntryIdentifier[] UntransferredGeneralLedgerEntryIdentifiers { get; }

		public PeriodContainsUntransferredEntriesException(Period period,
			GeneralLedgerEntryIdentifier[] untransferredGeneralLedgerEntryIdentifiers) : base(
			$"Period {period} contains un-transferred entries: {string.Join(", ", untransferredGeneralLedgerEntryIdentifiers)}") {
			Period = period;
			UntransferredGeneralLedgerEntryIdentifiers = untransferredGeneralLedgerEntryIdentifiers;
		}
	}
}

using System.Collections.Immutable;

namespace Transacto.Domain; 

public class PeriodContainsUntransferredEntriesException : Exception {
	public AccountingPeriod AccountingPeriod { get; }
	public ImmutableArray<GeneralLedgerEntryIdentifier> UntransferredGeneralLedgerEntryIdentifiers { get; }

	public PeriodContainsUntransferredEntriesException(AccountingPeriod accountingPeriod,
		ImmutableArray<GeneralLedgerEntryIdentifier> untransferredGeneralLedgerEntryIdentifiers) : base(
		$"Period {accountingPeriod} contains un-transferred entries: {string.Join(", ", untransferredGeneralLedgerEntryIdentifiers)}") {
		AccountingPeriod = accountingPeriod;
		UntransferredGeneralLedgerEntryIdentifiers = untransferredGeneralLedgerEntryIdentifiers;
	}
}

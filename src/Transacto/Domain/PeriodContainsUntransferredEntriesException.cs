using System.Collections.Immutable;

namespace Transacto.Domain;

public class PeriodContainsUntransferredEntriesException(
	AccountingPeriod accountingPeriod,
	ImmutableArray<GeneralLedgerEntryIdentifier> untransferredGeneralLedgerEntryIdentifiers)
	: Exception(
		$"Period {accountingPeriod} contains un-transferred entries: {string.Join(", ", untransferredGeneralLedgerEntryIdentifiers)}") {
	public AccountingPeriod AccountingPeriod { get; } = accountingPeriod;
	public ImmutableArray<GeneralLedgerEntryIdentifier> UntransferredGeneralLedgerEntryIdentifiers { get; } = untransferredGeneralLedgerEntryIdentifiers;
}

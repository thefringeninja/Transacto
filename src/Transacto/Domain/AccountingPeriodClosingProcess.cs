using System.Collections.Immutable;
using NodaTime;

namespace Transacto.Domain;

public class AccountingPeriodClosingProcess(
	ChartOfAccounts chartOfAccounts,
	AccountingPeriod accountingPeriod,
	LocalDateTime closingOn,
	IEnumerable<GeneralLedgerEntryIdentifier> generalLedgerEntryIdentifiers,
	GeneralLedgerEntryIdentifier closingGeneralLedgerEntryIdentifier,
	EquityAccount retainedEarningsAccount,
	AccountIsDeactivated accountIsDeactivated) {
	private readonly HashSet<GeneralLedgerEntryIdentifier> _generalLedgerEntryIdentifiers = new(generalLedgerEntryIdentifiers);

	public TrialBalance TrialBalance { get; } = new(chartOfAccounts);
	public ProfitAndLoss ProfitAndLoss { get; } = new(accountingPeriod, chartOfAccounts);

	public void TransferEntry(GeneralLedgerEntry generalLedgerEntry) {
		generalLedgerEntry.MustBeInBalance();
		generalLedgerEntry.MustBePosted();

		ProfitAndLoss.Transfer(generalLedgerEntry);
		TrialBalance.Transfer(generalLedgerEntry);
		TrialBalance.MustBeInBalance();
		_generalLedgerEntryIdentifiers.Remove(generalLedgerEntry.Identifier);
	}

	public GeneralLedgerEntry Complete() {
		MustNotContainUntransferredEntries();

		TrialBalance.MustBeInBalance();

		return ProfitAndLoss.GetClosingEntry(accountIsDeactivated, retainedEarningsAccount,
			closingOn, closingGeneralLedgerEntryIdentifier);
	}

	private void MustNotContainUntransferredEntries() {
		if (_generalLedgerEntryIdentifiers.Count > 0) {
			throw new PeriodContainsUntransferredEntriesException(accountingPeriod,
				_generalLedgerEntryIdentifiers.ToImmutableArray());
		}
	}
}

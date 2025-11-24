using System.Collections.Immutable;
using Transacto.Messages;

namespace Transacto.Domain;

public class PeriodContainsUntransferredEntriesException(
	AccountingPeriod accountingPeriod,
	ImmutableArray<GeneralLedgerEntryIdentifier> untransferredGeneralLedgerEntryIdentifiers)
	: Exception(
		FormatMessage(accountingPeriod, untransferredGeneralLedgerEntryIdentifiers).ToString()), IFormattableMessage {
	private static FormattableString FormatMessage(AccountingPeriod accountingPeriod,
		ImmutableArray<GeneralLedgerEntryIdentifier> untransferredGeneralLedgerEntryIdentifiers) =>
		$"Period {accountingPeriod} contains un-transferred entries: {string.Join(", ", untransferredGeneralLedgerEntryIdentifiers)}";

	public AccountingPeriod AccountingPeriod { get; } = accountingPeriod;

	public ImmutableArray<GeneralLedgerEntryIdentifier> UntransferredGeneralLedgerEntryIdentifiers { get; } =
		untransferredGeneralLedgerEntryIdentifiers;

	public FormattableString ToFormattableString() =>
		FormatMessage(AccountingPeriod, UntransferredGeneralLedgerEntryIdentifiers);
}

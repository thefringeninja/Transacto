using Transacto.Messages;

namespace Transacto.Domain;

public class GeneralLedgerEntryNotInBalanceException(GeneralLedgerEntryIdentifier generalLedgerEntryIdentifier)
	: Exception(FormatMessage(generalLedgerEntryIdentifier).ToString()), IFormattableMessage {
	private static FormattableString FormatMessage(GeneralLedgerEntryIdentifier generalLedgerEntryIdentifier) =>
		$"General ledger entry {generalLedgerEntryIdentifier.ToGuid():n} was not in balance.";

	public GeneralLedgerEntryIdentifier GeneralLedgerEntryIdentifier { get; } = generalLedgerEntryIdentifier;
	public FormattableString ToFormattableString() => FormatMessage(GeneralLedgerEntryIdentifier);
}

using Transacto.Messages;

namespace Transacto.Domain;

public class GeneralLedgerEntryWasPostedException(GeneralLedgerEntryIdentifier generalLedgerEntryIdentifier)
	: Exception(FormatMessage(generalLedgerEntryIdentifier).ToString()), IFormattableMessage {
	private static FormattableString FormatMessage(GeneralLedgerEntryIdentifier generalLedgerEntryIdentifier) =>
		$"General ledger entry {generalLedgerEntryIdentifier.ToGuid():n} was posted.";

	public GeneralLedgerEntryIdentifier GeneralLedgerEntryIdentifier { get; } = generalLedgerEntryIdentifier;
	public FormattableString ToFormattableString() => FormatMessage(GeneralLedgerEntryIdentifier);
}

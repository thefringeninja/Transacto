using Transacto.Messages;

namespace Transacto.Domain;

public class GeneralLedgerEntryWasNotPostedException(GeneralLedgerEntryIdentifier generalLedgerEntryIdentifier)
	: Exception(FormatMessage(generalLedgerEntryIdentifier).ToString()), IFormattableMessage {
	private static FormattableString FormatMessage(GeneralLedgerEntryIdentifier generalLedgerEntryIdentifier) =>
		$"The general ledger entry {generalLedgerEntryIdentifier.ToGuid():n} was not posted.";

	public GeneralLedgerEntryIdentifier GeneralLedgerEntryIdentifier { get; } = generalLedgerEntryIdentifier;


	public FormattableString ToFormattableString() => FormatMessage(GeneralLedgerEntryIdentifier);
}

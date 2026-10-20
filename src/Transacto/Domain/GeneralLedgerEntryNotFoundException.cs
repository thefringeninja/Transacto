using Transacto.Messages;

namespace Transacto.Domain;

public class GeneralLedgerEntryNotFoundException(GeneralLedgerEntryIdentifier generalLedgerEntryIdentifier)
	: Exception(FormatMessage(generalLedgerEntryIdentifier).ToString()), IFormattableMessage {
	public GeneralLedgerEntryIdentifier GeneralLedgerEntryIdentifier { get; } = generalLedgerEntryIdentifier;

	private static FormattableString FormatMessage(GeneralLedgerEntryIdentifier generalLedgerEntryIdentifier) =>
		$"General Ledger Entry {generalLedgerEntryIdentifier.ToGuid():n} was not found.";

	public FormattableString ToFormattableString() => FormatMessage(GeneralLedgerEntryIdentifier);
}

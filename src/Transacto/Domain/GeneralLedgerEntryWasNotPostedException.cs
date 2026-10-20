using Transacto.Messages;

namespace Transacto.Domain;

public class GeneralLedgerEntryWasNotPostedException(GeneralLedgerEntryIdentifier generalLedgerEntryIdentifier)
	: Exception(FormatMessage(generalLedgerEntryIdentifier).ToString()), IFormattableMessage {
	private static FormattableString FormatMessage(GeneralLedgerEntryIdentifier generalLedgerEntryIdentifier) =>
		$"The general ledger entry {generalLedgerEntryIdentifier.ToGuid():n} was not posted.";

	public GeneralLedgerEntryIdentifier GeneralLedgerEntryIdentifier { get; } = generalLedgerEntryIdentifier;


	public FormattableString ToFormattableString() => FormatMessage(GeneralLedgerEntryIdentifier);
}

public class GeneralLedgerEntryAlreadyAssignedSequenceNumberException(
	GeneralLedgerEntryIdentifier generalLedgerEntryIdentifier,
	GeneralLedgerEntrySequenceNumber expected,
	GeneralLedgerEntrySequenceNumber actual)
	: Exception(FormatMessage(generalLedgerEntryIdentifier, expected, actual).ToString()), IFormattableMessage {
	public GeneralLedgerEntryIdentifier GeneralLedgerEntryIdentifier { get; } = generalLedgerEntryIdentifier;
	public GeneralLedgerEntrySequenceNumber ExpectedSequenceNumber { get; } = expected;
	public GeneralLedgerEntrySequenceNumber ActualSequenceNumber { get; } = actual;

	private static FormattableString FormatMessage(GeneralLedgerEntryIdentifier generalLedgerEntryIdentifier,
		GeneralLedgerEntrySequenceNumber expected, GeneralLedgerEntrySequenceNumber actual) =>
		$"Attempted to assign a sequence number of {expected} to general ledger entry {generalLedgerEntryIdentifier.ToGuid():n} but it was already assigned a sequence number of {actual}.";

	public FormattableString ToFormattableString() =>
		FormatMessage(GeneralLedgerEntryIdentifier, ExpectedSequenceNumber, ActualSequenceNumber);
}

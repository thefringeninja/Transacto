namespace Transacto.Domain;

public class GeneralLedgerEntryWasPostedException(GeneralLedgerEntryIdentifier generalLedgerEntryIdentifier)
	: Exception("The general ledger entry was posted.") {
	public GeneralLedgerEntryIdentifier GeneralLedgerEntryIdentifier { get; } = generalLedgerEntryIdentifier;
}

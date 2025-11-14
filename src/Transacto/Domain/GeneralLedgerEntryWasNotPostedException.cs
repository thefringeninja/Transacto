namespace Transacto.Domain;

public class GeneralLedgerEntryWasNotPostedException(GeneralLedgerEntryIdentifier generalLedgerEntryIdentifier)
	: Exception("The general ledger entry was not posted.") {
	public GeneralLedgerEntryIdentifier GeneralLedgerEntryIdentifier { get; } = generalLedgerEntryIdentifier;
}

namespace Transacto.Domain; 

public class GeneralLedgerEntryWasNotPostedException : Exception {
	public GeneralLedgerEntryIdentifier GeneralLedgerEntryIdentifier { get; }

	public GeneralLedgerEntryWasNotPostedException(GeneralLedgerEntryIdentifier generalLedgerEntryIdentifier)
		: base("The general ledger entry was not posted.") {
		GeneralLedgerEntryIdentifier = generalLedgerEntryIdentifier;
	}
}

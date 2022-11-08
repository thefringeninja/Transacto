namespace Transacto.Domain; 

public class GeneralLedgerEntryNotFoundException : Exception {
	public GeneralLedgerEntryNotFoundException(GeneralLedgerEntryIdentifier generalLedgerEntryIdentifier)
		: base($"General Ledger Entry {generalLedgerEntryIdentifier} was not found.") {
	}
}

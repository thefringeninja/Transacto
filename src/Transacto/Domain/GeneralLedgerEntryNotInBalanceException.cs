namespace Transacto.Domain;

public class GeneralLedgerEntryNotInBalanceException(GeneralLedgerEntryIdentifier generalLedgerEntryIdentifier)
	: Exception("The general ledger entry was not in balance.") {
	public GeneralLedgerEntryIdentifier GeneralLedgerEntryIdentifier { get; } = generalLedgerEntryIdentifier;
}

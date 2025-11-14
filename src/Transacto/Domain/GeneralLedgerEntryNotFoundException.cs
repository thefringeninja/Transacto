namespace Transacto.Domain;

public class GeneralLedgerEntryNotFoundException(GeneralLedgerEntryIdentifier generalLedgerEntryIdentifier)
	: Exception($"General Ledger Entry {generalLedgerEntryIdentifier} was not found.");

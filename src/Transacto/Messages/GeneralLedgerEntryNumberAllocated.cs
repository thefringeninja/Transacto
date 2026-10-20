namespace Transacto.Messages;

partial record GeneralLedgerEntryNumberAllocated : IFormattableMessage {
	public FormattableString ToFormattableString() =>
		$"General ledger entry {GeneralLedgerEntryId} was a allocated a sequence number of {GeneralLedgerEntrySequenceNumber}";
}

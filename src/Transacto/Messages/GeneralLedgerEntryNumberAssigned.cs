namespace Transacto.Messages;

partial record GeneralLedgerEntryNumberAssigned : IFormattableMessage {
	public FormattableString ToFormattableString() =>
		$"General ledger entry {GeneralLedgerEntryId} was a assigned a sequence number of {GeneralLedgerEntrySequenceNumber}";
}

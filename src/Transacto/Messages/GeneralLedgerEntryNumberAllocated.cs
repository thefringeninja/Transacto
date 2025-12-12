namespace Transacto.Messages;

public record GeneralLedgerEntryNumberAssigned : IFormattableMessage {
	public required Guid GeneralLedgerEntryId { get; init; }
	public int GeneralLedgerEntrySequenceNumber { get; init; }
	
	public FormattableString ToFormattableString() =>
		$"General ledger entry {GeneralLedgerEntryId} was a assigned a sequence number of {GeneralLedgerEntrySequenceNumber}";
}

public record GeneralLedgerEntryNumberAssigne2d : IFormattableMessage {
	public required Guid GeneralLedgerEntryId { get; init; }
	public int GeneralLedgerEntrySequenceNumber { get; init; }
	
	public FormattableString ToFormattableString() =>
		$"General ledger entry {GeneralLedgerEntryId} was a assigned a sequence number of {GeneralLedgerEntrySequenceNumber}";
}

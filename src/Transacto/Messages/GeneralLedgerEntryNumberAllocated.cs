namespace Transacto.Messages;

public record GeneralLedgerEntryNumberAllocated : IFormattableMessage {
	public required Guid GeneralLedgerEntryId { get; init; }
	public required int GeneralLedgerEntrySequenceNumber { get; init; }
	
	public FormattableString ToFormattableString() =>
		$"General ledger entry {GeneralLedgerEntryId} was a allocated a sequence number of {GeneralLedgerEntrySequenceNumber}";
}

public record GeneralLedgerEntryNumberAssigned : IFormattableMessage {
	public required Guid GeneralLedgerEntryId { get; init; }
	public int GeneralLedgerEntrySequenceNumber { get; init; }
	
	public FormattableString ToFormattableString() =>
		$"General ledger entry {GeneralLedgerEntryId} was a assigned a sequence number of {GeneralLedgerEntrySequenceNumber}";
}

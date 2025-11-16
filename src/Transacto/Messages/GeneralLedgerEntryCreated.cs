namespace Transacto.Messages;

public record GeneralLedgerEntryCreated : IMessage {
	public required Guid GeneralLedgerEntryId { get; init; }
	public required string ReferenceNumber { get; init; }
	public required string CreatedOn { get; init; }
	public required string Period { get; init; }

	public (string template, object[] args) GetMessage() => (
		"General ledger entry {ReferenceNumber} was created in {Period}.", [ReferenceNumber, Period]);
}

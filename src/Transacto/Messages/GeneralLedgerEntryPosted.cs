namespace Transacto.Messages;

public record GeneralLedgerEntryPosted : IMessage {
	public required Guid GeneralLedgerEntryId { get; init; }
	public required string Period { get; init; }

	public (string template, object[] args) GetMessage() => ("General ledger entry was posted in {Period}.", [Period]);
}

namespace Transacto.Messages;

public record GeneralLedgerEntryPosted : IFormattableMessage {
	public required Guid GeneralLedgerEntryId { get; init; }
	public required string Period { get; init; }

	public FormattableString ToFormattableString() => $"General ledger entry was posted in accounting period {Period}.";
}

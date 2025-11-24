namespace Transacto.Messages;

public record GeneralLedgerEntryCreated : IFormattableMessage {
	public required Guid GeneralLedgerEntryId { get; init; }
	public required string ReferenceNumber { get; init; }
	public required string CreatedOn { get; init; }
	public required string Period { get; init; }

	public FormattableString ToFormattableString() => $"General ledger entry {ReferenceNumber} was created in accounting period {Period}.";
}

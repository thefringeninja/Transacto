namespace Transacto.Messages;

public record GeneralLedgerEntryCreated {
	public required Guid GeneralLedgerEntryId { get; init; }
	public required string ReferenceNumber { get; init; }
	public required string CreatedOn { get; init; }
	public required string Period { get; init; }
	public override string ToString() => $"General ledger entry {ReferenceNumber} was created in {Period}.";
}

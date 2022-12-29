namespace Transacto.Messages; 

public record GeneralLedgerEntryPosted {
	public required Guid GeneralLedgerEntryId { get; init; }
	public required string Period { get; init; }

	public override string ToString() => $"General ledger entry was posted in {Period}.";
}

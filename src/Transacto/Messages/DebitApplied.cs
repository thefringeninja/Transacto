namespace Transacto.Messages; 

public record DebitApplied {
	public required Guid GeneralLedgerEntryId { get; init; }
	public required decimal Amount { get; init; }
	public required int AccountNumber { get; init; }
	public override string ToString() => $"A debit of {Amount} was applied to {AccountNumber}.";
}

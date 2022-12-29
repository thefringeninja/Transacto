namespace Transacto.Messages;

public record CreditApplied {
	public required Guid GeneralLedgerEntryId { get; init; }
	public required decimal Amount { get; init; }
	public required int AccountNumber { get; init; }
	public override string ToString() => $"A credit of {Amount} was applied to {AccountNumber}.";
}

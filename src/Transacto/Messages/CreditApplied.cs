namespace Transacto.Messages;

public record CreditApplied : IMessage {
	public required Guid GeneralLedgerEntryId { get; init; }
	public required decimal Amount { get; init; }
	public required int AccountNumber { get; init; }

	public (string template, object[] args) GetMessage() =>
		("A credit of {Amount:F2} was applied to {AccountNumber}.", [Amount, AccountNumber]);
}

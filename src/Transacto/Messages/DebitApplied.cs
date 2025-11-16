namespace Transacto.Messages;

public record DebitApplied : IMessage {
	public required Guid GeneralLedgerEntryId { get; init; }
	public required decimal Amount { get; init; }
	public required int AccountNumber { get; init; }
	public (string template, object[] args) GetMessage() =>
		("A debit of {Amount:F2} was applied to account {AccountNumber}.", [Amount, AccountNumber]);
}

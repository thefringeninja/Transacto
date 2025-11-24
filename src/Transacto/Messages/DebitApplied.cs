namespace Transacto.Messages;

public record DebitApplied : IFormattableMessage {
	public required Guid GeneralLedgerEntryId { get; init; }
	public required decimal Amount { get; init; }
	public required int AccountNumber { get; init; }
	public FormattableString ToFormattableString() =>
		$"A debit of {Amount:N2} was applied to account {AccountNumber}.";
}

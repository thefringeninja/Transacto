namespace Transacto.Messages;

public record CreditApplied : IFormattableMessage {
	public required Guid GeneralLedgerEntryId { get; init; }
	public required decimal Amount { get; init; }
	public required int AccountNumber { get; init; }

	public FormattableString ToFormattableString() =>
		$"A credit of {Amount:N2} was applied to account {AccountNumber}.";
}

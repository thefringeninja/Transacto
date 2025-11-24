namespace Transacto.Messages;

public record AccountReactivated : IFormattableMessage {
	public required int AccountNumber { get; init; }

	public FormattableString ToFormattableString() =>
		$"Account {AccountNumber} was reactivated.";
}

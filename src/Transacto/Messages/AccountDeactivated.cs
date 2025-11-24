namespace Transacto.Messages;

public record AccountDeactivated : IFormattableMessage {
	public required int AccountNumber { get; init; }

	public FormattableString ToFormattableString() =>
		$"Account {AccountNumber} was deactivated.";
}

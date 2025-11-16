namespace Transacto.Messages;

public record AccountDeactivated : IMessage {
	public required int AccountNumber { get; init; }

	public (string template, object[] args) GetMessage() =>
		("Account {AccountNumber} was deactivated.", [AccountNumber]);
}

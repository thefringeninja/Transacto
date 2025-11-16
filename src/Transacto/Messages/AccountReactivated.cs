namespace Transacto.Messages;

public record AccountReactivated : IMessage {
	public required int AccountNumber { get; init; }

	public (string template, object[] args) GetMessage() =>
		("Account {AccountNumber} was reactivated.", [AccountNumber]);
}

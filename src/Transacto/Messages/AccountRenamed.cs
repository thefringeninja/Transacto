namespace Transacto.Messages;

public record AccountRenamed : IMessage {
	public required string NewAccountName { get; init; }
	public required int AccountNumber { get; init; }

	public (string template, object[] args) GetMessage() => ("Account {AccountNumber} was renamed to {NewAccountName}.",
		[AccountNumber, NewAccountName]);
}

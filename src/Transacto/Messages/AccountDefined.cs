using System.Runtime.CompilerServices;

namespace Transacto.Messages;

public record AccountDefined : IMessage {
	public required string AccountName { get; init; }
	public required int AccountNumber { get; init; }

	public (string template, object[] args) GetMessage() =>
		("Account {AccountNumber} - {AccountName} was defined.", [AccountNumber, AccountName]);
}

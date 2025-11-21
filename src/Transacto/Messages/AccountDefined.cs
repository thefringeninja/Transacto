namespace Transacto.Messages;

public record AccountDefined : IMessage {
	public required string AccountName { get; init; }
	public required int AccountNumber { get; init; }
	public required DefineAccountCashFlowCategory CashFlowCategory { get; init; }
	public required bool AffectsCash { get; init; }

	public (string template, object[] args) GetMessage() =>
		("Account {AccountNumber} - {AccountName} was defined.", [AccountNumber, AccountName]);
}

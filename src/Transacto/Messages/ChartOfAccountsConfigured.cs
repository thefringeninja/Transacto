namespace Transacto.Messages;

public record ChartOfAccountsConfigured : IMessage {
	public required int AccountNumberWidth { get; init; }

	public (string template, object[] args) GetMessage() => (
		$"Chart of Accounts configured: AccountNumberWidth = {AccountNumberWidth}.", [AccountNumberWidth]);
}

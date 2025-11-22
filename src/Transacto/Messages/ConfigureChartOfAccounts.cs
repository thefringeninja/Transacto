namespace Transacto.Messages;

public partial record ConfigureChartOfAccounts : IMessage {
	public (string template, object[] args) GetMessage() => (
		$"Configuring Chart of Accounts: AccountNumberWidth = {AccountNumberWidth}.", [AccountNumberWidth]);
}

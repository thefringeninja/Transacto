namespace Transacto.Messages;

public partial record ConfigureChartOfAccounts : IFormattableMessage {
	public FormattableString ToFormattableString() => 
		$"Configuring Chart of Accounts: AccountNumberWidth = {AccountNumberWidth}.";
}

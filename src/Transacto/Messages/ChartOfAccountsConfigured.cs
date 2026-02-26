namespace Transacto.Messages;

partial record ChartOfAccountsConfigured : IFormattableMessage {
	public FormattableString ToFormattableString() =>
		$"Chart of Accounts configured: AccountNumberWidth = {AccountNumberWidth}.";
}

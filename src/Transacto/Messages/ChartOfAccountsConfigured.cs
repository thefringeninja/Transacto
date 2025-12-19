namespace Transacto.Messages;

public record ChartOfAccountsConfigured : IFormattableMessage {
	public required int AccountNumberWidth { get; init; }
	
	public required int AccountsReceivable { get; init; }
	
	public required int AccountsPayable { get; init; }
	
	public required int RetainedEarnings { get; init; }

	public FormattableString ToFormattableString() =>
		$"Chart of Accounts configured: AccountNumberWidth = {AccountNumberWidth}.";
}

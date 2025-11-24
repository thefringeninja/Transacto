namespace Transacto.Messages;

public record ChartOfAccountsConfigured : IFormattableMessage {
	public required int AccountNumberWidth { get; init; }

	public FormattableString ToFormattableString() =>
		$"Chart of Accounts configured: AccountNumberWidth = {AccountNumberWidth}.";
}

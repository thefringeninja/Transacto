using Transacto.Domain;

namespace Transacto.Messages;

public record ChartOfAccountsConfigured : IFormattableMessage {
	public int AccountNumberWidth { get; init; } = Domain.AccountNumberWidth.Min.ToInt32();
	
	public required int AccountsReceivable { get; init; }
	
	public required int AccountsPayable { get; init; }
	
	public required int RetainedEarnings { get; init; }

	public FormattableString ToFormattableString() =>
		$"Chart of Accounts configured: AccountNumberWidth = {AccountNumberWidth}.";
}

namespace Transacto.Messages;

public partial record BeginClosingAccountingPeriod : IFormattableMessage {
	public FormattableString ToFormattableString() =>
		$"Begin closing current accounting period to equity account {RetainedEarningsAccountNumber} {ClosingOn:yyyy'-'MM'-'dd}.";
}

namespace Transacto.Messages;

public partial record DefineAccount : IFormattableMessage {
	public FormattableString ToFormattableString() =>
		$"Defining account {AccountNumber} - {AccountName}; Affects cash: {AffectsCash}; Cash Flow Category {CashFlowCategory}.";
}

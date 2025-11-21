namespace Transacto.Messages;

public partial record DefineAccount : IMessage {
	public (string template, object[] args) GetMessage() => (
		"Defining account {AccountNumber} - {AccountName}; Affects cash: {AffectsCash}; Cash Flow Category {CashFlowCategory}.",
		[AccountName, AccountNumber, AffectsCash, CashFlowCategory]);
}

namespace Transacto.Messages;

public partial record OpenGeneralLedger : IMessage {
	public (string template, object[] args) GetMessage() => ("Opening general ledger on {OpenedOn:O}.", [OpenedOn]);
}

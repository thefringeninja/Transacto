namespace Transacto.Messages;

public partial record OpenGeneralLedger : IFormattableMessage {
	public FormattableString ToFormattableString() => $"Opening general ledger on {OpenedOn:O}.";
}

namespace Transacto.Messages;

partial record GeneralLedgerOpened : IFormattableMessage {
	public FormattableString ToFormattableString() => $"The general ledger was opened on {OpenedOn:D}.";
}

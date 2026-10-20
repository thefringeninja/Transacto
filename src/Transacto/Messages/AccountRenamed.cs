namespace Transacto.Messages;

partial record AccountRenamed : IFormattableMessage {
	public FormattableString ToFormattableString() => $"Account {AccountNumber} was renamed to {NewAccountName}.";
}

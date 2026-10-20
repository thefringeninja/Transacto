namespace Transacto.Messages;

partial record AccountDefined : IFormattableMessage {
	public FormattableString ToFormattableString() => $"Account {AccountNumber} - {AccountName} was defined.";
}

namespace Transacto.Messages;

partial record AccountReactivated : IFormattableMessage {
	public FormattableString ToFormattableString() => $"Account {AccountNumber} was reactivated.";
}

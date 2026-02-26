namespace Transacto.Messages;

partial record AccountDeactivated : IFormattableMessage {
	public FormattableString ToFormattableString() => $"Account {AccountNumber} was deactivated.";
}

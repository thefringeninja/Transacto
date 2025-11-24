namespace Transacto.Messages;

public partial record DeactivateAccount : IFormattableMessage {
	public FormattableString ToFormattableString() => $"Deactivating account {AccountNumber}.";
}

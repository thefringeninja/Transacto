namespace Transacto.Messages;

public partial record ReactivateAccount : IFormattableMessage {
	public FormattableString ToFormattableString() => $"Reactivating account {AccountNumber}.";
}

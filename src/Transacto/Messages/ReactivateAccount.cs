namespace Transacto.Messages;

public partial record ReactivateAccount : IMessage {
	public (string template, object[] args) GetMessage() => ("Reactivating account {AccountNumber}.", [AccountNumber]);
}

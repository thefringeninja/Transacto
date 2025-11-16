namespace Transacto.Messages;

public partial record DeactivateAccount : IMessage {
	public (string template, object[] args) GetMessage() => ("Deactivating account {AccountNumber}.", [AccountNumber]);
}

namespace Transacto.Messages;

public partial record RenameAccount : IMessage {
	public (string template, object[] args) GetMessage() => ("Renaming account {AccountNumber} to {NewAccountName}.",
		[NewAccountName, AccountNumber]);
}

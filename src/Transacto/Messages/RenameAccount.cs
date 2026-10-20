namespace Transacto.Messages;

public partial record RenameAccount : IFormattableMessage {
	public FormattableString ToFormattableString() => $"Renaming account {AccountNumber} to {NewAccountName}.";
}

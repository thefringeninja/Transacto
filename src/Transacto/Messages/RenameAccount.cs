namespace Transacto.Messages; 

public partial record RenameAccount {
	public override string ToString() => $"Renaming account {NewAccountName}-{AccountNumber}.";
}

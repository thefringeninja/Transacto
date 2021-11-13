namespace Transacto.Messages; 

public partial record ReactivateAccount {
	public override string ToString() => $"Reactivating account {AccountNumber}.";
}
namespace Transacto.Messages; 

public partial record DeactivateAccount {
	public override string ToString() => $"Deactivating account {AccountNumber}.";
}
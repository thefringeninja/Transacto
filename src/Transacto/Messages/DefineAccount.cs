namespace Transacto.Messages; 

public partial record DefineAccount {
	public override string ToString() => $"Defining account {AccountName}-{AccountNumber}.";
}

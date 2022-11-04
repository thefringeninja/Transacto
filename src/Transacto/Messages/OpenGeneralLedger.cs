namespace Transacto.Messages; 

public partial record OpenGeneralLedger {
	public override string ToString() => $"Opening general ledger on {OpenedOn:O}.";
}
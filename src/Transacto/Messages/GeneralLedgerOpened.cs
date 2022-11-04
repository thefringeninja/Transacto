namespace Transacto.Messages; 

public record GeneralLedgerOpened {
	public string OpenedOn { get; init; } = default!;

	public override string ToString() => $"The general ledger was opened on {OpenedOn}.";
}
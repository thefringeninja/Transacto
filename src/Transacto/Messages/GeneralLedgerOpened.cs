namespace Transacto.Messages;

public record GeneralLedgerOpened {
	public required string OpenedOn { get; init; }

	public override string ToString() => $"The general ledger was opened on {OpenedOn}.";
}

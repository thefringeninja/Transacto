namespace Transacto.Messages; 

public record AccountRenamed {
	public required string NewAccountName  { get; init; }
	public required int AccountNumber { get; init; }

	public override string ToString() => $"Account {AccountNumber} was renamed to {NewAccountName}.";
}

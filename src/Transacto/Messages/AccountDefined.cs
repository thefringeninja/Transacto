namespace Transacto.Messages; 

public record AccountDefined {
	public required string AccountName  { get; init; }
	public required int AccountNumber { get; init; }

	public override string ToString() => $"Account {AccountNumber} - {AccountName} was defined.";
}

namespace Transacto.Messages {
    public record AccountDefined {
	    public string AccountName  { get; init; } = default!;
	    public int AccountNumber { get; init; }

        public override string ToString() => $"Account {AccountNumber} - {AccountName} was defined.";
    }
}

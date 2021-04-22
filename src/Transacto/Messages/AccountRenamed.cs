namespace Transacto.Messages {
    public record AccountRenamed {
	    public string NewAccountName  { get; init; } = default!;
	    public int AccountNumber { get; init; }

        public override string ToString() => $"Account {AccountNumber} was renamed to {NewAccountName}.";
    }
}

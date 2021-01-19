namespace Transacto.Messages {
    public record AccountRenamed {
	    public string NewAccountName = default!;
	    public int AccountNumber;

        public override string ToString() => $"Account {AccountNumber} was renamed to {NewAccountName}.";
    }
}

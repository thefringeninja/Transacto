namespace Transacto.Messages {
    public record AccountDefined {
	    public string AccountName = default!;
	    public int AccountNumber;

        public override string ToString() => $"Account {AccountNumber} - {AccountName} was defined.";
    }
}

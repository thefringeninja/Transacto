namespace Transacto.Messages {
    public record AccountReactivated {
	    public int AccountNumber { get; init; }

        public override string ToString() => $"Account {AccountNumber} was reactivated.";
    }
}

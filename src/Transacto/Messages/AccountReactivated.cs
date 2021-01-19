namespace Transacto.Messages {
    public record AccountReactivated {
	    public int AccountNumber;

        public override string ToString() => $"Account {AccountNumber} was reactivated.";
    }
}

namespace Transacto.Messages {
    public record AccountDeactivated {
	    public int AccountNumber;

        public override string ToString() => $"Account {AccountNumber} was deactivated.";
    }
}

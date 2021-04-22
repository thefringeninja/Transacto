namespace Transacto.Messages {
    public record AccountDeactivated {
	    public int AccountNumber { get; init; }

        public override string ToString() => $"Account {AccountNumber} was deactivated.";
    }
}

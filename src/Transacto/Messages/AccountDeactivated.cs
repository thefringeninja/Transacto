namespace Transacto.Messages {
    public class AccountDeactivated {
        public int AccountNumber { get; set; }

        public override string ToString() => $"Account {AccountNumber} was deactivated.";
    }
}

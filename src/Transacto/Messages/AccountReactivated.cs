namespace Transacto.Messages {
    public class AccountReactivated {
        public int AccountNumber { get; set; }

        public override string ToString() => $"Account {AccountNumber} was reactivated.";
    }
}

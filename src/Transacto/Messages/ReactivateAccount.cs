namespace Transacto.Messages {
    public class ReactivateAccount {
        public int AccountNumber { get; set; }

        public override string ToString() => $"Reactivating account {AccountNumber}.";
    }
}

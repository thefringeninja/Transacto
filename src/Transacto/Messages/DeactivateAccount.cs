namespace Transacto.Messages {
    public class DeactivateAccount {
        public int AccountNumber { get; set; }

        public override string ToString() => $"Deactivating account {AccountNumber}.";
    }
}

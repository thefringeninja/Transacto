namespace Transacto.Messages {
    partial record DeactivateAccount {
        public override string ToString() => $"Deactivating account {AccountNumber}.";
    }
}

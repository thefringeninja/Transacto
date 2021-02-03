namespace Transacto.Messages {
    partial record ReactivateAccount {
        public override string ToString() => $"Reactivating account {AccountNumber}.";
    }
}

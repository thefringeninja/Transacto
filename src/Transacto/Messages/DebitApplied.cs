using System;

namespace Transacto.Messages {
    public class DebitApplied {
        public Guid GeneralLedgerEntryId { get; set; }
        public decimal Amount { get; set; }
        public int AccountNumber { get; set; }

        public override string ToString() => $"A debit of {Amount} was applied to {AccountNumber}.";
    }
}

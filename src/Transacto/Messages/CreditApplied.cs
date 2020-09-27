using System;

namespace Transacto.Messages {
    public class CreditApplied {
        public Guid GeneralLedgerEntryId { get; set; }
        public decimal Amount { get; set; }
        public int AccountNumber { get; set; }

        public override string ToString() => $"A credit of {Amount} was applied to {AccountNumber}.";
    }
}

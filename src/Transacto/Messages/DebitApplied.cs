using System;

namespace Transacto.Messages {
	public record DebitApplied {
		public Guid GeneralLedgerEntryId;
		public decimal Amount;
		public int AccountNumber;
	    public override string ToString() => $"A debit of {Amount} was applied to {AccountNumber}.";
    }
}

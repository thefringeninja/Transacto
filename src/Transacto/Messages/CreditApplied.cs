using System;

namespace Transacto.Messages {
    public record CreditApplied {
	    public Guid GeneralLedgerEntryId;
	    public decimal Amount;
	    public int AccountNumber;
	    public override string ToString() => $"A credit of {Amount} was applied to {AccountNumber}.";
    }
}

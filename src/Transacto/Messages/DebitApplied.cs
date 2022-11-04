using System;

namespace Transacto.Messages; 

public record DebitApplied {
	public Guid GeneralLedgerEntryId { get; init; }
	public decimal Amount { get; init; }
	public int AccountNumber { get; init; }
	public override string ToString() => $"A debit of {Amount} was applied to {AccountNumber}.";
}
using System;

namespace Transacto.Messages; 

public record CreditApplied {
	public Guid GeneralLedgerEntryId { get; init; }
	public decimal Amount { get; init; }
	public int AccountNumber { get; init; }
	public override string ToString() => $"A credit of {Amount} was applied to {AccountNumber}.";
}
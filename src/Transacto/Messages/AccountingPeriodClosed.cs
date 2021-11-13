using System;

namespace Transacto.Messages; 

public record AccountingPeriodClosed {
	public string Period  { get; init; } = default!;
	public Guid[] GeneralLedgerEntryIds  { get; init; } = Array.Empty<Guid>();
	public BalanceLineItem[] Balance  { get; init; } = Array.Empty<BalanceLineItem>();
	public Guid ClosingGeneralLedgerEntryId { get; init; }

	public override string ToString() => $"Accounting period {Period} has been closed.";
}
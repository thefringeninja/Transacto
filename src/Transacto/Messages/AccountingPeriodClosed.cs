using System.Collections.Immutable;

namespace Transacto.Messages; 

public record AccountingPeriodClosed {
	public required string Period  { get; init; } = default!;
	public required ImmutableArray<Guid> GeneralLedgerEntryIds  { get; init; }
	public required ImmutableArray<BalanceLineItem> Balance  { get; init; }
	public required Guid ClosingGeneralLedgerEntryId { get; init; }

	public override string ToString() => $"Accounting period {Period} has been closed.";
}

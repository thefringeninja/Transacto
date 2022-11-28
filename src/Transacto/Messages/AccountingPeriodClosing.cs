using System.Collections.Immutable;

namespace Transacto.Messages; 

public record AccountingPeriodClosing {
	public required string Period { get; init; }
	public required ImmutableArray<Guid> GeneralLedgerEntryIds { get; init; }
	public required string ClosingOn { get; init; }
	public required int RetainedEarningsAccountNumber { get; init; }
	public required Guid ClosingGeneralLedgerEntryId { get; init; }

	public override string ToString() => $"Closing accounting period {Period}.";
}

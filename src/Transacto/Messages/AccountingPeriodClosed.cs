using System.Collections.Immutable;

namespace Transacto.Messages;

public record AccountingPeriodClosed : IFormattableMessage {
	public required string Period { get; init; }
	public required ImmutableArray<Guid> GeneralLedgerEntryIds { get; init; }
	public required ImmutableArray<BalanceLineItem> Balance { get; init; }
	public required Guid ClosingGeneralLedgerEntryId { get; init; }

	public FormattableString ToFormattableString() => $"Accounting period {Period} has been closed.";
}

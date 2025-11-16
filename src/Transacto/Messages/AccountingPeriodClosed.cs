using System.Collections.Immutable;

namespace Transacto.Messages;

public record AccountingPeriodClosed : IMessage {
	public required string Period { get; init; }
	public required ImmutableArray<Guid> GeneralLedgerEntryIds { get; init; }
	public required ImmutableArray<BalanceLineItem> Balance { get; init; }
	public required Guid ClosingGeneralLedgerEntryId { get; init; }

	public (string template, object[] args) GetMessage() => ("Accounting period {Period} has been closed.", [Period]);
}

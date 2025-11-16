using System.Collections.Immutable;

namespace Transacto.Messages;

public record AccountingPeriodClosing : IMessage {
	public required string Period { get; init; }
	public required ImmutableArray<Guid> GeneralLedgerEntryIds { get; init; }
	public required string ClosingOn { get; init; }
	public required int RetainedEarningsAccountNumber { get; init; }
	public required Guid ClosingGeneralLedgerEntryId { get; init; }

	public (string template, object[] args) GetMessage() => ("Accounting period {Period} is closing.", [Period]);
}

using System.Collections.Immutable;
using NodaTime;

namespace Transacto.Messages;

public record AccountingPeriodClosing : IFormattableMessage {
	public required string Period { get; init; }
	public required ImmutableArray<Guid> GeneralLedgerEntryIds { get; init; }
	public required LocalDate ClosingOn { get; init; }
	public required int RetainedEarningsAccountNumber { get; init; }
	public required Guid ClosingGeneralLedgerEntryId { get; init; }

	public FormattableString ToFormattableString() => $"Accounting period {Period} is closing.";
}

using NodaTime;

namespace Transacto.Messages;

public record GeneralLedgerEntryCreated : IFormattableMessage {
	public required Guid GeneralLedgerEntryId { get; init; }
	public required LocalDate CreatedOn { get; init; }
	public required string Period { get; init; }

	public FormattableString ToFormattableString() => $"General ledger entry {GeneralLedgerEntryId} was created in accounting period {Period}.";
}

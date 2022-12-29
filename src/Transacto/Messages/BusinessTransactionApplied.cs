using System.Text.Json;

namespace Transacto.Messages;

public record BusinessTransactionApplied {
	public required Guid GeneralLedgerEntryId { get; init; }
	public required string ReferenceNumber { get; init; }
	public required JsonDocument Document { get; init; }
}

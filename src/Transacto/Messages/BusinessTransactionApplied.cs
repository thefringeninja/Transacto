using System;
using System.Text.Json;

namespace Transacto.Messages {
	public record BusinessTransactionApplied {
		public Guid GeneralLedgerEntryId { get; init; }
		public string ReferenceNumber { get; init; } = default!;
		public JsonDocument Document { get; init; } = default!;
	}
}

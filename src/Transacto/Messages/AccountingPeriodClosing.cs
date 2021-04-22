using System;

namespace Transacto.Messages {
	public record AccountingPeriodClosing {
		public string Period { get; init; } = default!;
		public Guid[] GeneralLedgerEntryIds { get; init; } = Array.Empty<Guid>();
		public DateTimeOffset ClosingOn { get; init; }
		public int RetainedEarningsAccountNumber { get; init; }
		public Guid ClosingGeneralLedgerEntryId { get; init; }

		public override string ToString() => $"Closing accounting period {Period}.";
	}
}

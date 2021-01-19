using System;

namespace Transacto.Messages {
	public record AccountingPeriodClosing {
		public string Period = default!;
		public Guid[] GeneralLedgerEntryIds = Array.Empty<Guid>();
		public DateTimeOffset ClosingOn;
		public int RetainedEarningsAccountNumber;
		public Guid ClosingGeneralLedgerEntryId;

		public override string ToString() => $"Closing accounting period {Period}.";
	}
}

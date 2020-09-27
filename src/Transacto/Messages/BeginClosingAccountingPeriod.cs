using System;

namespace Transacto.Messages {
	public class BeginClosingAccountingPeriod {
		public Guid[] GeneralLedgerEntryIds { get; set; } = Array.Empty<Guid>();
		public DateTimeOffset ClosingOn { get; set; }
		public int RetainedEarningsAccountNumber { get; set; }
		public Guid ClosingGeneralLedgerEntryId { get; set; }

		public override string ToString() => $"Closing accounting period.";
	}
}

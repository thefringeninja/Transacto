using System;

namespace Transacto.Messages {
	public class AccountingPeriodClosing {
		public string Period { get; set; } = null!;
		public Guid[] GeneralLedgerEntryIds { get; set; } = Array.Empty<Guid>();
		public DateTimeOffset ClosingOn { get; set; }
		public int RetainedEarningsAccountNumber { get; set; }
		public Guid ClosingGeneralLedgerEntryId { get; set; }
	}
}

using System;

namespace Transacto.Messages {
	public class AccountingPeriodClosed {
		public PeriodDto Period { get; set; } = null!;
		public Guid[] GeneralLedgerEntryIds { get; set; } = Array.Empty<Guid>();
	}
}

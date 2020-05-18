using System;

namespace Transacto.Messages {
	public class AccountingPeriodClosing {
		public PeriodDto Period { get; set; } = null!;
		public Guid[] GeneralLedgerEntryIds { get; set; } = Array.Empty<Guid>();
	}
}

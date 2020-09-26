using System;

namespace Transacto.Messages {
	public class AccountingPeriodClosed {
		public string Period { get; set; } = null!;
		public Guid[] GeneralLedgerEntryIds { get; set; } = Array.Empty<Guid>();
		public BalanceLineItem[] Balance { get; set; } = Array.Empty<BalanceLineItem>();
		public Guid ClosingGeneralLedgerEntryId { get; set; }
	}
}

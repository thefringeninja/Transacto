using System;

namespace Transacto.Messages {
	public record AccountingPeriodClosed {
		public string Period = default!;
		public Guid[] GeneralLedgerEntryIds = Array.Empty<Guid>();
		public BalanceLineItem[] Balance = Array.Empty<BalanceLineItem>();
		public Guid ClosingGeneralLedgerEntryId;

		public override string ToString() => $"Accounting period {Period} has been closed.";
	}
}

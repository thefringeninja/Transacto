using System;
using System.Collections.Generic;

namespace Transacto.Messages {
	public class AccountingPeriodClosed {
		public string Period { get; set; } = null!;
		public Guid[] GeneralLedgerEntryIds { get; set; } = Array.Empty<Guid>();
		public Dictionary<int, decimal> Balance { get; set; } = new Dictionary<int, decimal>();
		public Guid ClosingGeneralLedgerEntryId { get; set; }
	}
}

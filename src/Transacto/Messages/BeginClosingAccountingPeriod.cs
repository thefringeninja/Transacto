using System;

namespace Transacto.Messages {
	partial class BeginClosingAccountingPeriod {
		public BeginClosingAccountingPeriod() {
			GeneralLedgerEntryIds = Array.Empty<Guid>();
		}

		public override string ToString() => "Closing accounting period.";
	}
}

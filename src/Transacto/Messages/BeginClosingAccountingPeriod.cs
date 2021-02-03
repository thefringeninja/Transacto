using System;

namespace Transacto.Messages {
	partial record BeginClosingAccountingPeriod {
		public BeginClosingAccountingPeriod() {
			GeneralLedgerEntryIds = Array.Empty<Guid>();
		}

		public override string ToString() => "Closing accounting period.";
	}
}

using System;

namespace Transacto.Domain {
	public class GeneralLedgerEntryNotInPeriodException : Exception {
		public GeneralLedgerEntryNotInPeriodException(GeneralLedgerEntryNumber number, DateTimeOffset createdOn,
			Period period) : base(
			$"General ledger entry {number} had a creation date of {createdOn}, but the current period is {period}") {
		}
	}
}

using System;
using NodaTime;

namespace Transacto.Domain {
	public class GeneralLedgerEntryNotInPeriodException : Exception {
		public GeneralLedgerEntryNumber Number { get; }
		public LocalDateTime CreatedOn { get; }
		public Period Period { get; }

		public GeneralLedgerEntryNotInPeriodException(GeneralLedgerEntryNumber number, LocalDateTime createdOn,
			Period period) : base(
			$"General ledger entry {number} had a creation date of {createdOn}, but the current period is {period}") {
			Number = number;
			CreatedOn = createdOn;
			Period = period;
		}
	}
}

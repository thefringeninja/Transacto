using System;
using NodaTime;

namespace Transacto.Domain {
	public class GeneralLedgerEntryNotInPeriodException : Exception {
		public GeneralLedgerEntryNumber Number { get; }
		public LocalDateTime CreatedOn { get; }
		public AccountingPeriod AccountingPeriod { get; }

		public GeneralLedgerEntryNotInPeriodException(GeneralLedgerEntryNumber number, LocalDateTime createdOn,
			AccountingPeriod accountingPeriod) : base(
			$"General ledger entry {number} had a creation date of {createdOn}, but the current period is {accountingPeriod}") {
			Number = number;
			CreatedOn = createdOn;
			AccountingPeriod = accountingPeriod;
		}
	}
}

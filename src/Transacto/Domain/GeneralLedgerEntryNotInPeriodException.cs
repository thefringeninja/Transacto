using NodaTime;

namespace Transacto.Domain;

public class GeneralLedgerEntryNotInPeriodException(
	GeneralLedgerEntryNumber number,
	LocalDateTime createdOn,
	AccountingPeriod accountingPeriod)
	: Exception(
		$"General ledger entry {number} had a creation date of {createdOn}, but the current period is {accountingPeriod}") {
	public GeneralLedgerEntryNumber Number { get; } = number;
	public LocalDateTime CreatedOn { get; } = createdOn;
	public AccountingPeriod AccountingPeriod { get; } = accountingPeriod;
}

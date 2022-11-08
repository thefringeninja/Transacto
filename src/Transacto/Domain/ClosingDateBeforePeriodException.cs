using NodaTime;

namespace Transacto.Domain; 

public class ClosingDateBeforePeriodException : Exception {
	public AccountingPeriod AccountingPeriod { get; }
	public LocalDate Date { get; }

	public ClosingDateBeforePeriodException(AccountingPeriod accountingPeriod, LocalDate date)
		: base($"Closing date {date} is before period {accountingPeriod}.") {
		AccountingPeriod = accountingPeriod;
		Date = date;
	}
}

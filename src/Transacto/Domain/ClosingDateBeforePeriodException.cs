using NodaTime;

namespace Transacto.Domain;

public class ClosingDateBeforePeriodException(AccountingPeriod accountingPeriod, LocalDate date)
	: Exception($"Closing date {date} is before period {accountingPeriod}.") {
	public AccountingPeriod AccountingPeriod { get; } = accountingPeriod;
	public LocalDate Date { get; } = date;
}

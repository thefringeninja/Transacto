using NodaTime;
using Transacto.Messages;

namespace Transacto.Domain;

public class ClosingDateBeforePeriodException(AccountingPeriod accountingPeriod, LocalDate date)
	: Exception(FormatMessage(accountingPeriod, date).ToString()), IFormattableMessage {
	private static FormattableString FormatMessage(AccountingPeriod accountingPeriod, LocalDate date) =>
		$"Closing date {date} is before period {accountingPeriod.ToString()}.";

	public AccountingPeriod AccountingPeriod { get; } = accountingPeriod;
	public LocalDate Date { get; } = date;
	public FormattableString ToFormattableString() => FormatMessage(AccountingPeriod, Date);
}

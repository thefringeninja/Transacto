using System;
using NodaTime;

namespace Transacto.Domain {
	public class ClosingDateBeforePeriodException : Exception {
		public Period Period { get; }
		public LocalDate Date { get; }

		public ClosingDateBeforePeriodException(Period period, LocalDate date)
			: base($"Closing date {date} is before period {period}.") {
			Period = period;
			Date = date;
		}
	}
}

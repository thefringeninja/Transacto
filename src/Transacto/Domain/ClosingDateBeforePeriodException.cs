using System;

namespace Transacto.Domain {
	public class ClosingDateBeforePeriodException : Exception {
		public Period Period { get; }
		public DateTimeOffset Date { get; }

		public ClosingDateBeforePeriodException(Period period, DateTimeOffset date)
			: base($"Closing date {date:O} is before period {period}.") {
			Period = period;
			Date = date;
		}
	}
}

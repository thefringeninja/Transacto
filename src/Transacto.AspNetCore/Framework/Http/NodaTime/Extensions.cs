// ReSharper disable once CheckNamespace

namespace NodaTime;

public static class Extensions {
	extension(Period) {
		public static Period Monthly => Period.FromMonths(1);
		public static Period Weekly => Period.FromWeeks(1);
		public static Period Quarterly => Period.FromMonths(3);
		public static Period Yearly => Period.FromYears(1);
	}
	extension(LocalDate value) {
		public LocalDate FirstDayOfNextQuarter {
			get {
				// Calculate the month to add to reach the *next* quarter's start month.
				return Get(value);

				static LocalDate Get(LocalDate value) {
					int monthsToAdd = 3 - (value.Month - 1) % 3;

					// Add the calculated months to get a date *within* the next quarter, then get the first of that month/year.
					return value.PlusMonths(monthsToAdd).ToYearMonth().OnDayOfMonth(1);
				}
			}
		}

		public LocalDate StartOfThisWeek {
			get {
				while (value.DayOfWeek != IsoDayOfWeek.Sunday) {
					value = value.PlusDays(-1);
				}

				return value;
			}
		}
	}
}

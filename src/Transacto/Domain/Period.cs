using System;

namespace Transacto.Domain {
	public readonly struct Period : IEquatable<Period>, IComparable<Period> {
		public static readonly Period Empty = default;
		public int Month { get; }
		public int Year { get; }

		public static bool TryParse(string period, out Period value) {
			if (string.IsNullOrEmpty(period) || period.Length != 6 ||
			    !int.TryParse(period[4..], out var month) ||
			    !int.TryParse(period[..4], out var year)) {
				value = default;
				return false;
			}

			value = new Period(month, year);
			return true;
		}

		public static Period Parse(string value) =>
			TryParse(value, out var period)
				? period
				: throw new FormatException();

		public static Period Open(DateTimeOffset dateTimeOffset) =>
			new(dateTimeOffset.UtcDateTime.Month, dateTimeOffset.UtcDateTime.Year);

		private Period(int month, int year) {
			MustBeAMonth(month);

			Month = month;
			Year = year;
		}

		private static void MustBeAMonth(int month) {
			if (month < 1 || month > 12) {
				throw new ArgumentOutOfRangeException(nameof(month));
			}
		}

		public Period Next() => Month == 12
			? new Period(1, Year + 1)
			: new Period(Month + 1, Year);

		public bool Contains(DateTimeOffset dateTimeOffset) =>
			dateTimeOffset.UtcDateTime.Month == Month && dateTimeOffset.UtcDateTime.Year == Year;

		public void MustNotBeAfter(DateTimeOffset closingOn) {
			if (closingOn.UtcDateTime < new DateTime(Year, Month, 1, 0, 0, 0, DateTimeKind.Utc)) {
				throw new ClosingDateBeforePeriodException(this, closingOn);
			}
		}

		public int CompareTo(Period other) {
			var yearComparison = Year.CompareTo(other.Year);
			return yearComparison != 0 ? yearComparison : Month.CompareTo(other.Month);
		}

		public bool Equals(Period other) => Month == other.Month && Year == other.Year;
		public override bool Equals(object? obj) => obj is Period other && Equals(other);
		public static bool operator ==(Period left, Period right) => left.Equals(right);
		public static bool operator !=(Period left, Period right) => !left.Equals(right);
		public override string ToString() => $"{Year:D4}{Month:D2}";
		public override int GetHashCode() => HashCode.Combine(Month, Year);
		public static bool operator <(Period left, Period right) => left.CompareTo(right) < 0;
		public static bool operator >(Period left, Period right) => left.CompareTo(right) > 0;
		public static bool operator <=(Period left, Period right) => left.CompareTo(right) <= 0;
		public static bool operator >=(Period left, Period right) => left.CompareTo(right) >= 0;
	}
}

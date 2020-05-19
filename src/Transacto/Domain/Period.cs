using System;

namespace Transacto.Domain {
	public readonly struct Period : IEquatable<Period> {
		public static readonly Period Empty = default;
		public int Month { get; }
		public int Year { get; }

		public static bool TryParse(string period, out Period value) {
			if (string.IsNullOrEmpty(period) || period.Length != 6 ||
			    !int.TryParse(period[..2], out var month) || NotAMonth(month) ||
			    !int.TryParse(period[2..], out var year)) {
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
			new Period(dateTimeOffset.UtcDateTime.Month, dateTimeOffset.UtcDateTime.Year);

		private Period(int month, int year) {
			if (NotAMonth(month)) {
				throw new ArgumentOutOfRangeException(nameof(month));
			}

			Month = month;
			Year = year;
		}

		private static bool NotAMonth(int month) => month < 1 || month > 12;

		public Period Next() => Month == 12
			? new Period(1, Year + 1)
			: new Period(Month + 1, Year);

		public bool Contains(DateTimeOffset dateTimeOffset) =>
			dateTimeOffset.UtcDateTime.Month == Month && dateTimeOffset.UtcDateTime.Year == Year;

		public void MustNotBeAfter(DateTimeOffset closingOn) {
			if (closingOn.UtcDateTime < new DateTime(Year, Month, 1)) {
				throw new InvalidOperationException();
			}
		}

		public bool Equals(Period other) => Month == other.Month && Year == other.Year;
		public override bool Equals(object? obj) => obj is Period other && Equals(other);
		public static bool operator ==(Period left, Period right) => left.Equals(right);
		public static bool operator !=(Period left, Period right) => !left.Equals(right);
		public override string ToString() => $"{Month:D2}{Year:D4}";
		public override int GetHashCode() => HashCode.Combine(Month, Year);
	}
}

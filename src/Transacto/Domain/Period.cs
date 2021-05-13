using System;
using NodaTime;
using NodaTime.Text;

namespace Transacto.Domain {
	public readonly struct Period : IEquatable<Period>, IComparable<Period> {
		public static readonly Period Empty = default;
		private static readonly YearMonthPattern Pattern = YearMonthPattern.CreateWithInvariantCulture("yyyyMM");

		private readonly YearMonth _value;


		public static bool TryParse(string period, out Period value) {
			if (string.IsNullOrEmpty(period) || period.Length != 6 ||
			    !int.TryParse(period[4..], out var month) ||
			    !int.TryParse(period[..4], out var year)) {
				value = default;
				return false;
			}

			value = new Period(new YearMonth(year, month));
			return true;
		}

		public static Period Parse(string value) =>
			TryParse(value, out var period)
				? period
				: throw new FormatException();

		public static Period Open(LocalDate value) => new(value.ToYearMonth());

		private Period(YearMonth value) => _value = value;

		public Period Next() => new(_value.OnDayOfMonth(1).Plus(NodaTime.Period.FromMonths(1)).ToYearMonth());

		public bool Contains(LocalDate date) => date.ToYearMonth() == _value;

		public void MustNotBeAfter(LocalDate date) {
			if (date < _value.OnDayOfMonth(1)) {
				throw new ClosingDateBeforePeriodException(this, date);
			}
		}

		public int CompareTo(Period other) => _value.CompareTo(other._value);

		public bool Equals(Period other) => _value.Equals(other._value);
		public override bool Equals(object? obj) => obj is Period other && Equals(other);
		public static bool operator ==(Period left, Period right) => left.Equals(right);
		public static bool operator !=(Period left, Period right) => !left.Equals(right);
		public override int GetHashCode() => _value.GetHashCode();
		public static bool operator <(Period left, Period right) => left.CompareTo(right) < 0;
		public static bool operator >(Period left, Period right) => left.CompareTo(right) > 0;
		public static bool operator <=(Period left, Period right) => left.CompareTo(right) <= 0;
		public static bool operator >=(Period left, Period right) => left.CompareTo(right) >= 0;

		public override string ToString() => Pattern.Format(_value);
	}
}

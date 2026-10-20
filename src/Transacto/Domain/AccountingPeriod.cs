using NodaTime;
using NodaTime.Text;

namespace Transacto.Domain;

public readonly struct AccountingPeriod : IEquatable<AccountingPeriod>, IComparable<AccountingPeriod> {
	public static readonly AccountingPeriod Empty = default;
	private static readonly YearMonthPattern Pattern = YearMonthPattern.CreateWithInvariantCulture("yyyyMM");

	private readonly YearMonth _value;
	
	public int Year => _value.Year;
	public int Month => _value.Month;
	
	public LocalDate FirstDay { get; }
	public LocalDate LastDay { get; }

	public LocalDate FirstDayOfQuarter { get; }
	public LocalDate LastDayOfQuarter { get; }
	
	public int Quarter => _value.Month switch {
		< 4 => 1,
		>= 4 and < 7 => 2,
		>= 7 and < 10 => 3,
		_ => 4
	};

	public static bool TryParse(string? period, out AccountingPeriod value) {
		if (string.IsNullOrEmpty(period) || period.Length != 6 ||
		    !int.TryParse(period[4..], out var month) ||
		    !int.TryParse(period[..4], out var year)) {
			value = default;
			return false;
		}

		value = new AccountingPeriod(new YearMonth(year, month));
		return true;
	}

	public static AccountingPeriod Parse(string value) {
		ArgumentNullException.ThrowIfNull(value);
		return TryParse(value, out var period)
			? period
			: throw new FormatException();
	}

	public static AccountingPeriod Open(LocalDate value) => new(value.ToYearMonth());

	private AccountingPeriod(YearMonth value) {
		var quarter = value.Month switch {
			< 4 => 1,
			>= 4 and < 7 => 2,
			>= 7 and < 10 => 3,
			_ => 4
		};
		_value = value;
		FirstDay = value.OnDayOfMonth(1);
		LastDay = FirstDay.PlusMonths(1).PlusDays(-1);
		LastDayOfQuarter = new LocalDate(value.Year, quarter * 3, 1).PlusMonths(1).PlusDays(-1);
		FirstDayOfQuarter = FirstDay.PlusMonths(-3).PlusDays(1);
	}

	public AccountingPeriod Next() => new(_value.OnDayOfMonth(1).Plus(Period.FromMonths(1)).ToYearMonth());

	public bool Contains(LocalDate date) => date.ToYearMonth() == _value;

	public void MustNotBeAfter(LocalDate date) {
		if (date < _value.OnDayOfMonth(1)) {
			throw new ClosingDateBeforePeriodException(this, date);
		}
	}

	public int CompareTo(AccountingPeriod other) => _value.CompareTo(other._value);

	public bool Equals(AccountingPeriod other) => _value.Equals(other._value);
	public override bool Equals(object? obj) => obj is AccountingPeriod other && Equals(other);
	public static bool operator ==(AccountingPeriod left, AccountingPeriod right) => left.Equals(right);
	public static bool operator !=(AccountingPeriod left, AccountingPeriod right) => !left.Equals(right);
	public override int GetHashCode() => _value.GetHashCode();
	public static bool operator <(AccountingPeriod left, AccountingPeriod right) => left.CompareTo(right) < 0;
	public static bool operator >(AccountingPeriod left, AccountingPeriod right) => left.CompareTo(right) > 0;
	public static bool operator <=(AccountingPeriod left, AccountingPeriod right) => left.CompareTo(right) <= 0;
	public static bool operator >=(AccountingPeriod left, AccountingPeriod right) => left.CompareTo(right) >= 0;

	public override string ToString() => Pattern.Format(_value);
}

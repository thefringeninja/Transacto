using AutoFixture;
using NodaTime;

namespace Transacto.Domain;

public class AccountingPeriodTests {
	[AutoFixtureData]
	public void Equality(AccountingPeriod sut) {
		var copy = AccountingPeriod.Parse(sut.ToString());
		Assert.Equal(sut, copy);
	}

	[AutoFixtureData]
	public void EqualityOperator(AccountingPeriod sut) {
		var copy = AccountingPeriod.Parse(sut.ToString());
		Assert.True(sut == copy);
	}

	[AutoFixtureData]
	public void InequalityOperator(AccountingPeriod sut) {
		Assert.True(sut != sut.Next());
	}

	[AutoFixtureData]
	public void NextReturnsExpectedResult(AccountingPeriod accountingPeriod) {
		var sut = accountingPeriod.Next();
		Assert.True(sut > accountingPeriod);
		Assert.True(sut < sut.Next());
		Assert.True(sut >= accountingPeriod);
		Assert.True(sut <= sut.Next());
	}

	[AutoFixtureData]
	public void DateNotInPeriodThrows(LocalDateTime value) {
		var period = AccountingPeriod.Open(value.Date);
		var ex = Assert.Throws<ClosingDateBeforePeriodException>(() =>
			period.MustNotBeAfter(value.PlusMonths(-1).Date));
		Assert.Equal(value.PlusMonths(-1).Date, ex.Date);
		Assert.Equal(period, ex.AccountingPeriod);
	}

	public static IEnumerable<object[]> ContainsCases() {
		var fixture = new ScenarioFixture();
		var date = fixture.Create<LocalDate>().ToYearMonth().OnDayOfMonth(1);

		var period = AccountingPeriod.Open(date);

		var daysInMonth = CalendarSystem.Iso.GetDaysInMonth(date.Year, date.Month);

		for (var day = 0; day < daysInMonth; day++) {
			yield return new object[] { period, date.PlusDays(day), true };
		}

		yield return new object[] { period, date.PlusDays(daysInMonth), false };

		yield return new object[] { period, date.PlusDays(-1), false };
	}

	[MemberData(nameof(ContainsCases))]
	public void ContainsReturnsExpectedResult(AccountingPeriod accountingPeriod, LocalDate value, bool expected) {
		Assert.Equal(expected, accountingPeriod.Contains(value));
	}

	public static IEnumerable<object[]> MonthOutOfRangeCases() {
		yield return new object[] { 0 };
		yield return new object[] { 13 };
	}

	[MemberData(nameof(MonthOutOfRangeCases))]
	public void MonthOutOfRangeThrows(int month) {
		var ex = Assert.Throws<ArgumentOutOfRangeException>(() => AccountingPeriod.Parse($"2020{month:D2}"));
		Assert.Equal("month", ex.ParamName);
	}

	public static IEnumerable<object[]> InvalidValueCases() {
		yield return new object[] { string.Empty };
		yield return new object[] { "a" };
		yield return new object[] { "0" };
		yield return new object[] { "aaaaaa" };
		yield return new object[] { "0110000" };
	}

	[MemberData(nameof(InvalidValueCases))]
	public void ParseInvalidValueReturnsExpectedResult(string value) {
		Assert.Throws<FormatException>(() => AccountingPeriod.Parse(value));
	}

	[AutoFixtureData]
	public void TryParseValidValueReturnsExpectedResult(AccountingPeriod accountingPeriod) {
		Assert.True(AccountingPeriod.TryParse(accountingPeriod.ToString(), out var sut));
		Assert.Equal(accountingPeriod, sut);
	}

	[MemberData(nameof(InvalidValueCases))]
	public void TryParseInvalidValueReturnsExpectedResult(string value) {
		Assert.False(AccountingPeriod.TryParse(value, out var sut));
		Assert.Equal(default, sut);
	}

	[AutoFixtureData]
	public void ToStringReturnsExpectedResult(LocalDate value) {
		var actual = AccountingPeriod.Open(value).ToString();
		Assert.Equal($"{value.Year:D4}{value.Month:D2}", actual);
	}
}

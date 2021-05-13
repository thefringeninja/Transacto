using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AutoFixture;
using NodaTime;
using Xunit;

namespace Transacto.Domain {
	public class PeriodTests {
		[Theory, AutoTransactoData]
		public void Equality(Period sut) {
			var copy = Period.Parse(sut.ToString());
			Assert.Equal(sut, copy);
		}

		[Theory, AutoTransactoData]
		public void EqualityOperator(Period sut) {
			var copy = Period.Parse(sut.ToString());
			Assert.True(sut == copy);
		}

		[Theory, AutoTransactoData]
		public void InequalityOperator(Period left, Period right) {
			Assert.False(left == right);
		}

		[Theory, AutoTransactoData]
		public void NextReturnsExpectedResult(Period period) {
			var sut = period.Next();
			Assert.True(sut > period);
			Assert.True(sut < sut.Next());
		}

		[Theory, AutoTransactoData]
		public void DateNotInPeriodThrows(LocalDateTime value) {
			var period = Period.Open(value.Date);
			var ex = Assert.Throws<ClosingDateBeforePeriodException>(() =>
				period.MustNotBeAfter(value.PlusMonths(-1).Date));
			Assert.Equal(value.PlusMonths(-1).Date, ex.Date);
			Assert.Equal(period, ex.Period);
		}

		public static IEnumerable<object[]> ContainsCases() {
			var fixture = new ScenarioFixture();
			var date = fixture.Create<LocalDate>().ToYearMonth().OnDayOfMonth(1);

			var period = Period.Open(date);

			var daysInMonth = CalendarSystem.Iso.GetDaysInMonth(date.Year, date.Month);

			for (var day = 0; day < daysInMonth; day++) {
				yield return new object[] {period, date.PlusDays(day), true};
			}

			yield return new object[] {period, date.PlusDays(daysInMonth), false};

			yield return new object[] {period, date.PlusDays(-1), false};

		}

		[Theory, MemberData(nameof(ContainsCases))]
		public void ContainsReturnsExpectedResult(Period period, LocalDate value, bool expected) {
			Assert.Equal(expected, period.Contains(value));
		}

		public static IEnumerable<object[]> MonthOutOfRangeCases() {
			yield return new object[] {0};
			yield return new object[] {13};
		}

		[Theory, MemberData(nameof(MonthOutOfRangeCases))]
		public void MonthOutOfRangeThrows(int month) {
			var ex = Assert.Throws<ArgumentOutOfRangeException>(() => Period.Parse($"2020{month:D2}"));
			Assert.Equal("month", ex.ParamName);
		}

		public static IEnumerable<object[]> InvalidValueCases() {
			yield return new object[] {string.Empty};
			yield return new object[] {"a"};
			yield return new object[] {"0"};
			yield return new object[] {"aaaaaa"};
			yield return new object[] {"0110000"};
		}

		[Theory, MemberData(nameof(InvalidValueCases))]
		public void ParseInvalidValueReturnsExpectedResult(string value) {
			Assert.Throws<FormatException>(() => Period.Parse(value));
		}

		[Theory, AutoTransactoData]
		public void TryParseValidValueReturnsExpectedResult(Period period) {
			Assert.True(Period.TryParse(period.ToString(), out var sut));
			Assert.Equal(period, sut);
		}

		[Theory, MemberData(nameof(InvalidValueCases))]
		public void TryParseInvalidValueReturnsExpectedResult(string value) {
			Assert.False(Period.TryParse(value, out var sut));
			Assert.Equal(default, sut);
		}

		[Theory, AutoTransactoData]
		public void ToStringReturnsExpectedResult(LocalDate value) {
			var actual = Period.Open(value).ToString();
			Assert.Equal($"{value.Year:D4}{value.Month:D2}", actual);
		}
	}
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AutoFixture;
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
		public void DateNotInPeriodThrows(DateTimeOffset value) {
			var period = Period.Open(value);
			var ex = Assert.Throws<ClosingDateBeforePeriodException>(() => period.MustNotBeAfter(value.AddMonths(-1)));
			Assert.Equal(value.AddMonths(-1), ex.Date);
			Assert.Equal(period, ex.Period);
		}

		public static IEnumerable<object[]> ContainsCases() {
			var fixture = new ScenarioFixture();
			var period = fixture.Create<Period>();

			var daysInMonth = CultureInfo.InvariantCulture.Calendar.GetDaysInMonth(period.Year, period.Month);
			foreach (var day in Enumerable.Range(1, daysInMonth)) {
				yield return new object[]
					{period, new DateTimeOffset(new DateTime(period.Year, period.Month, day), TimeSpan.Zero), true};
			}

			yield return new object[] {
				period,
				new DateTimeOffset(new DateTime(period.Year, period.Month, daysInMonth).AddDays(1), TimeSpan.Zero),
				false
			};

			yield return new object[]
				{period, new DateTimeOffset(new DateTime(period.Year, period.Month, 1).AddDays(-1), TimeSpan.Zero), false};

			yield return new object[]
				{period, new DateTimeOffset(new DateTime(period.Year, period.Month, 1), TimeSpan.FromHours(-1)), true};

			yield return new object[]
				{period, new DateTimeOffset(new DateTime(period.Year, period.Month, 1), TimeSpan.FromHours(1)), true};

		}

		[Theory, MemberData(nameof(ContainsCases))]
		public void ContainsReturnsExpectedResult(Period period, DateTimeOffset value, bool expected) {
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
		public void ToStringReturnsExpectedResult(Period sut) {
			var actual = sut.ToString();
			Assert.Equal($"{sut.Year:D4}{sut.Month:D2}", actual);
		}
	}
}

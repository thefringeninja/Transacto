namespace Transacto.Domain;

public class MoneyTests : ValueObjectTests<Money> {
	public void Zero() => Assert.Equal(new Money(0m), Money.Zero);

	public static IEnumerable<object[]> ComparisonCases() {
		yield return [new Money(0m), new Money(0m), 0];
		yield return [new Money(-1m), new Money(0m), -1];
		yield return [new Money(1m), new Money(0m), 1];
	}

	[MemberData(nameof(ComparisonCases))]
	public void ComparisonReturnsExpectedResult(Money left, Money right, int expected) =>
		Assert.Equal(left.CompareTo(right), expected);

	public static IEnumerable<object[]> GreaterThanCases() {
		yield return [1m, 1m, false];
		yield return [1m, 0m, true];
		yield return [1m, 2m, false];
	}

	[MemberData(nameof(GreaterThanCases))]
	public void GreaterThanReturnsExpectedResult(decimal left, decimal right, bool gt) =>
		Assert.Equal(new Money(left) > new Money(right), gt);

	public static IEnumerable<object[]> GreaterThanOrEqualCases() {
		yield return [1m, 1m, true];
		yield return [1m, 0m, true];
		yield return [1m, 2m, false];
	}

	[MemberData(nameof(GreaterThanOrEqualCases))]
	public void GreaterThanOrEqualThanReturnsExpectedResult(decimal left, decimal right, bool gte) =>
		Assert.Equal(new Money(left) >= new Money(right), gte);

	public static IEnumerable<object[]> LessThanCases() {
		yield return [1m, 1m, false];
		yield return [1m, 0m, false];
		yield return [1m, 2m, true];
	}

	[MemberData(nameof(LessThanCases))]
	public void LessThanReturnsExpectedResult(decimal left, decimal right, bool le) =>
		Assert.Equal(new Money(left) < new Money(right), le);

	public static IEnumerable<object[]> LessThanOrEqualCases() {
		yield return [1m, 1m, true];
		yield return [1m, 0m, false];
		yield return [1m, 2m, true];
	}

	[MemberData(nameof(LessThanOrEqualCases))]
	public void LessThanOrEqualThanReturnsExpectedResult(decimal left, decimal right, bool lte) =>
		Assert.Equal(new Money(left) <= new Money(right), lte);

	[AutoFixtureData]
	public void MoneyAdditionOperator(decimal left, decimal right) =>
		Assert.Equal(new Money(left + right), new Money(left) + new Money(right));

	[AutoFixtureData]
	public void DecimalAdditionOperator(decimal left, decimal right) =>
		Assert.Equal(new Money(left + right), new Money(left) + right);

	[AutoFixtureData]
	public void MoneySubtractionOperator(decimal left, decimal right) =>
		Assert.Equal(new Money(left - right), new Money(left) - new Money(right));

	[AutoFixtureData]
	public void DecimalSubtractionOperator(decimal left, decimal right) =>
		Assert.Equal(new Money(left - right), new Money(left) - right);

	[AutoFixtureData]
	public void NegationOperator(decimal value) => Assert.Equal(new Money(-value), -new Money(value));

	[AutoFixtureData]
	public void ToDecimalReturnsExpectedResult(decimal value) => Assert.Equal(value, new Money(value).ToDecimal());

	[AutoFixtureData]
	public void MultiplyingByDecimalReturnsExpectedResult(decimal money, decimal percentage) =>
		Assert.Equal(new Money(money * percentage), new Money(money) * percentage);
	
	[AutoFixtureData]
	public void MultiplyingByPercentageReturnsExpectedResult(decimal money, decimal percentage) =>
		Assert.Equal(new Money(money * percentage), new Money(money) * new Percentage(percentage));

}

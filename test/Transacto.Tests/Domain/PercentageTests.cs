namespace Transacto.Domain;

public class PercentageTests : ValueObjectTests<Percentage> {
	public void Zero() => Assert.Equal(new Percentage(0m), Percentage.Zero);

	public static IEnumerable<object[]> ComparisonCases() {
		yield return [new Percentage(0m), new Percentage(0m), 0];
		yield return [new Percentage(0m), new Percentage(1m), -1];
		yield return [new Percentage(1m), new Percentage(0m), 1];
	}

	[MemberData(nameof(ComparisonCases))]
	public void ComparisonReturnsExpectedResult(Percentage left, Percentage right, int expected) =>
		Assert.Equal(left.CompareTo(right), expected);

	public static IEnumerable<object[]> GreaterThanCases() {
		yield return [1m, 1m, false];
		yield return [1m, 0m, true];
		yield return [1m, 2m, false];
	}

	[MemberData(nameof(GreaterThanCases))]
	public void GreaterThanReturnsExpectedResult(decimal left, decimal right, bool gt) =>
		Assert.Equal(new Percentage(left) > new Percentage(right), gt);

	public static IEnumerable<object[]> GreaterThanOrEqualCases() {
		yield return [1m, 1m, true];
		yield return [1m, 0m, true];
		yield return [1m, 2m, false];
	}

	[MemberData(nameof(GreaterThanOrEqualCases))]
	public void GreaterThanOrEqualThanReturnsExpectedResult(decimal left, decimal right, bool gte) =>
		Assert.Equal(new Percentage(left) >= new Percentage(right), gte);

	public static IEnumerable<object[]> LessThanCases() {
		yield return [1m, 1m, false];
		yield return [1m, 0m, false];
		yield return [1m, 2m, true];
	}

	[MemberData(nameof(LessThanCases))]
	public void LessThanReturnsExpectedResult(decimal left, decimal right, bool le) =>
		Assert.Equal(new Percentage(left) < new Percentage(right), le);

	public static IEnumerable<object[]> LessThanOrEqualCases() {
		yield return [1m, 1m, true];
		yield return [1m, 0m, false];
		yield return [1m, 2m, true];
	}

	[MemberData(nameof(LessThanOrEqualCases))]
	public void LessThanOrEqualThanReturnsExpectedResult(decimal left, decimal right, bool lte) =>
		Assert.Equal(new Percentage(left) <= new Percentage(right), lte);


	[AutoFixtureData]
	public void ToDecimalReturnsExpectedResult(decimal value) => Assert.Equal(value, new Percentage(value).ToDecimal());

	[AutoFixtureData]
	public void LessThanZeroThrows(decimal value) =>
		Assert.Throws<ArgumentOutOfRangeException>(() => new Percentage(-Math.Abs(value)));
}

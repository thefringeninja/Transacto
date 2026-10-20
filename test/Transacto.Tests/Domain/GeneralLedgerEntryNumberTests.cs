using AutoFixture;
using AutoFixture.Idioms;

namespace Transacto.Domain;

public class GeneralLedgerEntryNumberTests : ValueObjectTests<GeneralLedgerEntryNumber> {
	public void ParseIsGuarded() => Fixture.Create<GuardClauseAssertion>()
		.Verify(typeof(AccountingPeriod).GetMethod(nameof(AccountingPeriod.Parse)));

	[AutoFixtureData]
	public void ParseValidValueReturnsExpectedResult(GeneralLedgerEntryNumber number) {
		var sut = GeneralLedgerEntryNumber.Parse(number.ToString());
		Assert.Equal(number, sut);
	}

	[AutoFixtureData]
	public void TryParseValidValueReturnsExpectedResult(GeneralLedgerEntryNumber number) {
		Assert.True(GeneralLedgerEntryNumber.TryParse(number.ToString(), out var sut));
		Assert.Equal(number, sut);
	}

	public static IEnumerable<object[]> ParseInvalidValueTestCases() {
		yield return [string.Empty];
		yield return ["a"];
		yield return ["a-"];
		yield return ["a--1"];
		yield return ["-1"];
		yield return [" "];
	}

	[MemberData(nameof(ParseInvalidValueTestCases))]
	public void ParseInvalidValueReturnsExpectedResult(string value) {
		Assert.Throws<FormatException>(() => GeneralLedgerEntryNumber.Parse(value));
	}

	[MemberData(nameof(ParseInvalidValueTestCases))]
	public void TryParseInvalidValueReturnsExpectedResult(string value) {
		Assert.False(GeneralLedgerEntryNumber.TryParse(value, out var sut));
		Assert.Equal(default, sut);
	}
}

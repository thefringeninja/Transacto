using AutoFixture;

namespace Transacto.Domain;

public class GeneralLedgerEntryPrefixTests : ValueObjectTests<GeneralLedgerEntryNumberPrefix> {
	public static IEnumerable<object[]> InvalidPrefixCases() {
		yield return [" "];
		yield return [string.Empty];
		yield return [" a"];
		yield return ["a "];
		yield return ["a\t"];
	}

	[MemberData(nameof(InvalidPrefixCases))]
	public void InvalidPrefix(string value) {
		var ex = Assert.Throws<ArgumentException>(() => new GeneralLedgerEntryNumberPrefix(value));
		Assert.Equal(nameof(value), ex.ParamName);
	}

	public static IEnumerable<object[]> PrefixCasesTooLongCases() {
		var fixture = new Fixture();
		yield return [new string('a', GeneralLedgerEntryNumberPrefix.MaxPrefixLength + 1)];
		for (var i = 0; i < 3; i++)
			yield return [new string('a', GeneralLedgerEntryNumberPrefix.MaxPrefixLength + fixture.Create<int>())];
	}

	[MemberData(nameof(PrefixCasesTooLongCases))]
	public void PrefixTooLong(string value) {
		var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new GeneralLedgerEntryNumberPrefix(value));
		Assert.Equal("value.Length", ex.ParamName);
	}

	[AutoFixtureData]
	public void ToStringReturnsExpectedResult(Guid guid) {
		var value = guid.ToString()[..5];
		var sut = new GeneralLedgerEntryNumberPrefix(value);

		Assert.Equal(value, sut.ToString());
	}
}

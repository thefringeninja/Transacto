namespace Transacto.Domain;

public class GeneralLedgerEntryPrefixTests : ValueObjectTests<GeneralLedgerEntryNumberPrefix> {
	public static IEnumerable<object[]> InvalidPrefixCases() {
		yield return [" "];
		yield return [string.Empty];
		yield return [" a"];
		yield return ["a "];
		yield return ["a\t"];
		yield return [new string('a', GeneralLedgerEntryNumberPrefix.MaxPrefixLength + 1)];
	}

	[MemberData(nameof(InvalidPrefixCases))]
	public void InvalidPrefix(string value) {
		var ex = Assert.Throws<ArgumentException>(() => new GeneralLedgerEntryNumberPrefix(value));
		Assert.Equal(nameof(value), ex.ParamName);
	}
	
	[AutoFixtureData]
	public void ToStringReturnsExpectedResult(Guid guid) {
		var value = guid.ToString()[..5];
		var sut = new GeneralLedgerEntryNumberPrefix(value);

		Assert.Equal(value, sut.ToString());
	}
}

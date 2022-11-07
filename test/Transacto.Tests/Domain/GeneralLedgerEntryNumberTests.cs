namespace Transacto.Domain; 

public class GeneralLedgerEntryPrefixTests {
	public static IEnumerable<object[]> InvalidPrefixCases() {
		yield return new object[] { " " };
		yield return new object[] { string.Empty };
		yield return new object[] { " a" };
		yield return new object[] { "a " };
		yield return new object[] { "a\t" };
		yield return new object[] { new string('a', GeneralLedgerEntryNumberPrefix.MaxPrefixLength + 1) };
	}

	[MemberData(nameof(InvalidPrefixCases))]
	public void InvalidPrefix(string value) {
		var ex = Assert.Throws<ArgumentException>(() => new GeneralLedgerEntryNumberPrefix(value));
		Assert.Equal(nameof(value), ex.ParamName);
	}

	[AutoFixtureData]
	public void Equality(GeneralLedgerEntryNumberPrefix sut) {
		var copy = sut;
		Assert.Equal(sut, copy);
	}

	[AutoFixtureData]
	public void EqualityOperator(GeneralLedgerEntryNumberPrefix sut) {
		var copy = sut;
		Assert.True(sut == copy);
	}

	[AutoFixtureData]
	public void InequalityOperator(GeneralLedgerEntryNumberPrefix left, GeneralLedgerEntryNumberPrefix right) {
		Assert.True(left != right);
	}

	[AutoFixtureData]
	public void ToStringReturnsExpectedResult(Guid guid) {
		var value = guid.ToString()[..5];
		var sut = new GeneralLedgerEntryNumberPrefix(value);

		Assert.Equal(value, sut.ToString());
	}
}

public class GeneralLedgerEntryNumberTests {
	[AutoFixtureData]
	public void Equality(GeneralLedgerEntryNumber sut) {
		var copy = new GeneralLedgerEntryNumber(sut.Prefix, sut.SequenceNumber);
		Assert.Equal(sut, copy);
	}

	[AutoFixtureData]
	public void EqualityOperator(GeneralLedgerEntryNumber sut) {
		var copy = new GeneralLedgerEntryNumber(sut.Prefix, sut.SequenceNumber);
		Assert.True(sut == copy);
	}

	[AutoFixtureData]
	public void InequalityOperator(GeneralLedgerEntryNumber left, GeneralLedgerEntryNumber right) {
		Assert.True(left != right);
	}

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
		yield return new object[] { string.Empty };
		yield return new object[] { "a" };
		yield return new object[] { "a-" };
		yield return new object[] { "a--1" };
		yield return new object[] { "-1" };
		yield return new object[] { " " };
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

namespace Transacto.Domain;

public class AccountNameTests : ValueObjectTests<AccountName> {
	public static IEnumerable<object[]> InvalidAccountNameCases() {
		yield return [string.Empty];
		yield return [new string('a', AccountName.MaxLength + 1)];
	}

	[MemberData(nameof(InvalidAccountNameCases))]
	public void InvalidAccountNameThrows(string value) {
		var ex = Assert.Throws<ArgumentException>(() => new AccountName(value));
		Assert.Equal("value", ex.ParamName);
	}

	[AutoFixtureData]
	public void ToStringReturnsExpectedResult(string expected) {
		var sut = new AccountName(expected);
		var actual = sut.ToString();
		Assert.Equal(expected, actual);
	}
}

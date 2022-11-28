namespace Transacto.Domain;

public class AccountNameTests {
	[AutoFixtureData]
	public void Equality(AccountName sut) {
		var copy = new AccountName(sut.ToString());
		Assert.Equal(sut, copy);
	}

	[AutoFixtureData]
	public void EqualityOperator(AccountName sut) {
		var copy = new AccountName(sut.ToString());

		Assert.True(sut == copy);
	}

	[AutoFixtureData]
	public void InequalityOperator(AccountName sut, AccountName other) {
		Assert.True(sut != other);
	}

	public static IEnumerable<object[]> InvalidAccountNameCases() {
		yield return new object[] { string.Empty };
		yield return new object[] { new string('a', AccountName.MaxLength + 1) };
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

namespace Transacto.Domain;

public class AccountNumberTests {
	[AutoFixtureData]
	public void Equality(AccountNumber sut) {
		var copy = new AccountNumber(sut.ToInt32());
		Assert.Equal(sut, copy);
	}

	[AutoFixtureData]
	public void EqualityOperator(AccountNumber sut) {
		var copy = new AccountNumber(sut.ToInt32());
		Assert.True(sut == copy);
	}

	[AutoFixtureData]
	public void InequalityOperator(AccountNumber sut, AccountNumber other) {
		Assert.False(sut == other);
	}

	public static IEnumerable<object[]> InvalidAccountNumberCases() {
		yield return new object[] { -1 };
		yield return new object[] { 999 };
		yield return new object[] { 9000 };
		yield return new object[] { int.MaxValue };
	}

	[MemberData(nameof(InvalidAccountNumberCases))]
	public void InvalidAccountNumberThrows(int value) {
		var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new AccountNumber(value));
		Assert.Equal("value", ex.ParamName);
	}

	[AutoFixtureData]
	public void ToInt32ReturnsExpectedResult(int value) {
		var expected = Math.Max(1000, value % 8999);
		var sut = new AccountNumber(expected);
		Assert.Equal(expected, sut.ToInt32());
	}

	[AutoFixtureData]
	public void ToStringReturnsExpectedResult(int value) {
		var expected = Math.Max(1000, value % 8999);
		var sut = new AccountNumber(expected);
		Assert.Equal(expected.ToString(), sut.ToString());
	}
}

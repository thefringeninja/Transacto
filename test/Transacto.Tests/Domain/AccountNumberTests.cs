namespace Transacto.Domain;

public class AccountNumberTests : ValueObjectTests<AccountNumber> {
	public static IEnumerable<object[]> InvalidAccountNumberCases() {
		yield return [-1];
		yield return [999];
		yield return [9000];
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

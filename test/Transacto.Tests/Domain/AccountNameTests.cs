using System;
using System.Collections.Generic;
using Xunit;

namespace Transacto.Domain; 

public class AccountNameTests {
	[Theory, AutoTransactoData]
	public void Equality(AccountName sut) {
		var copy = new AccountName(sut.ToString());
		Assert.Equal(sut, copy);
	}

	[Theory, AutoTransactoData]
	public void EqualityOperator(AccountName sut) {
		var copy = new AccountName(sut.ToString());

		Assert.True(sut == copy);
	}

	[Theory, AutoTransactoData]
	public void InequalityOperator(AccountName sut, AccountName other) {
		Assert.True(sut != other);
	}

	public static IEnumerable<object[]> InvalidAccountNameCases() {
		yield return new object[]{string.Empty};
		yield return new object[]{new string('a', AccountName.MaxLength + 1)};
	}

	[Theory, MemberData(nameof(InvalidAccountNameCases))]
	public void InvalidAccountNameThrows(string value) {
		var ex = Assert.Throws<ArgumentException>(() => new AccountName(value));
		Assert.Equal("value", ex.ParamName);
	}

	[Theory, AutoTransactoData]
	public void ToStringReturnsExpectedResult(string expected) {
		var sut = new AccountName(expected);
		var actual = sut.ToString();
		Assert.Equal(expected, actual);
	}
}
using AutoFixture;

namespace Transacto.Domain;

public class AccountNumberWidthTests : ValueObjectTests<AccountNumberWidth> {
	public static IEnumerable<object[]> AccountNumberWidthTooShortCases() {
		var fixture = new Fixture();
		yield return [AccountNumberWidth.Min.ToInt32() - 1];
		for (var i = 0; i < 3; i++) {
			yield return [AccountNumberWidth.Min.ToInt32() - fixture.Create<int>()];
		}
	}
	
	[MemberData(nameof(AccountNumberWidthTooShortCases))]
	public void AccountNumberWidthTooShort(int length) =>
		Assert.Throws<ArgumentOutOfRangeException>(() => new AccountNumberWidth(length));
	
	public static IEnumerable<object[]> AccountNumberWidthTooLongCases() {
		var fixture = new Fixture();
		yield return [AccountNumberWidth.Max.ToInt32() + 1];
		for (var i = 0; i < 3; i++) {
			yield return [AccountNumberWidth.Max.ToInt32() + fixture.Create<int>()];
		}
	}
	
	[MemberData(nameof(AccountNumberWidthTooLongCases))]
	public void AccountNumberWidthTooLong(int length) =>
		Assert.Throws<ArgumentOutOfRangeException>(() => new AccountNumberWidth(length));
}

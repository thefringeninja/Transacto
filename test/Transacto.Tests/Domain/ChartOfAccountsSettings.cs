using AutoFixture;

namespace Transacto.Domain;

public class ChartOfAccountsSettingsTests : ValueObjectTests<ChartOfAccountsSettings> {
	public static IEnumerable<object[]> AccountNumberWidthTooShortCases() {
		var fixture = new Fixture();
		yield return [ChartOfAccountsSettings.MinAccountNumberWidth - 1];
		for (var i = 0; i < 3; i++) {
			yield return [ChartOfAccountsSettings.MinAccountNumberWidth - fixture.Create<int>()];
		}
	}
	
	[MemberData(nameof(AccountNumberWidthTooShortCases))]
	public void AccountNumberWidthTooShort(int length) =>
		Assert.Throws<ArgumentOutOfRangeException>(() => new ChartOfAccountsSettings(length));
	
	public static IEnumerable<object[]> AccountNumberWidthTooLongCases() {
		var fixture = new Fixture();
		yield return [ChartOfAccountsSettings.MaxAccountNumberWidth + 1];
		for (var i = 0; i < 3; i++) {
			yield return [ChartOfAccountsSettings.MaxAccountNumberWidth + fixture.Create<int>()];
		}
	}
	
	[MemberData(nameof(AccountNumberWidthTooLongCases))]
	public void AccountNumberWidthTooLong(int length) =>
		Assert.Throws<ArgumentOutOfRangeException>(() => new ChartOfAccountsSettings(length));
}

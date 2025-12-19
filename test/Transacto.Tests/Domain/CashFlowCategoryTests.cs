namespace Transacto.Domain;
using Transacto.Messages;

public class CashFlowCategoryTests : ValueObjectTests<CashFlowCategory> {
	private static IEnumerable<object[]> ToStringCases() {
		yield return [CashFlowCategory.Operations, nameof(CashFlowCategory.Operations)];
		yield return [CashFlowCategory.Investment, nameof(CashFlowCategory.Investment)];
		yield return [CashFlowCategory.Finance, nameof(CashFlowCategory.Finance)];
	}

	[MemberData(nameof(ToStringCases))]
	public void ToStringReturnsExpectedResult(CashFlowCategory sut, string expected) =>
		Assert.Equal(expected, sut.ToString());

	private static IEnumerable<object[]> ToEnumCases() {
		yield return [CashFlowCategory.Operations, CashFlowCategoryType.Operations];
		yield return [CashFlowCategory.Investment, CashFlowCategoryType.Investment];
		yield return [CashFlowCategory.Finance, CashFlowCategoryType.Finance];
	}

	[MemberData(nameof(ToEnumCases))]
	public void ToEnumReturnsExpectedResult(CashFlowCategory sut, CashFlowCategoryType expected) =>
		Assert.Equal(expected, sut.ToEnum());
}

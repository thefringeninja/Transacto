using Transacto.Messages;

namespace Transacto.Domain;

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
		yield return [CashFlowCategory.Operations, DefineAccountCashFlowCategory.Operations];
		yield return [CashFlowCategory.Investment, DefineAccountCashFlowCategory.Investment];
		yield return [CashFlowCategory.Finance, DefineAccountCashFlowCategory.Finance];
	}

	[MemberData(nameof(ToEnumCases))]
	public void ToEnumReturnsExpectedResult(CashFlowCategory sut, DefineAccountCashFlowCategory expected) =>
		Assert.Equal(expected, sut.ToEnum());
}

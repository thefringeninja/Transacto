namespace Transacto.Domain;

public class AccountCannotBeCashAccountException(AccountNumber accountNumber)
	: Exception($"Account {accountNumber} cannot be a cash account") {
	public AccountNumber AccountNumber { get; } = accountNumber;
}

public class AccountExpectedCashFlowCategoryException(
	AccountNumber accountNumber,
	CashFlowCategory actualCashFlowCategory,
	params CashFlowCategory[] expectedCashFlowCategories) : Exception(
	$"Account {accountNumber} expected Cash Flow Category {actualCashFlowCategory} to be one of [{string.Join(",", expectedCashFlowCategories)}]") {
	public AccountNumber AccountNumber { get; } = accountNumber;
	public CashFlowCategory ActualCashFlowCategory { get; } = actualCashFlowCategory;
	public CashFlowCategory[] ExpectedCashFlowCategories { get; } = expectedCashFlowCategories;
}

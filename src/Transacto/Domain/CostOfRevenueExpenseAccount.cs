namespace Transacto.Domain;

public record CostOfRevenueExpenseAccount : ExpenseAccount {
	public CostOfRevenueExpenseAccount(AccountName accountName, AccountNumber accountNumber, int accountNumberWidth,
		CashFlowCategory cashFlowCategory) : base(accountName, accountNumber, cashFlowCategory,
		CreateAccountNumberSpace(5, accountNumberWidth)) {
		CashFlowCategoryMustBe(accountNumber, cashFlowCategory, CashFlowCategory.Operations);
	}
}

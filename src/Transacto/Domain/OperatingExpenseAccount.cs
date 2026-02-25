namespace Transacto.Domain;

public record OperatingExpenseAccount : ExpenseAccount {
	public OperatingExpenseAccount(AccountName accountName, AccountNumber accountNumber, AccountNumberWidth accountNumberWidth,
		CashFlowCategory cashFlowCategory) : base(accountName, accountNumber, cashFlowCategory,
		CreateAccountNumberSpace(6, accountNumberWidth)) {
		CashFlowCategoryMustBe(accountNumber, cashFlowCategory, CashFlowCategory.Operations);
	}
}

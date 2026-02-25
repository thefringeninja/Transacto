namespace Transacto.Domain;

public record OtherExpenseAccount : ExpenseAccount {
	public OtherExpenseAccount(AccountName accountName, AccountNumber accountNumber, AccountNumberWidth accountNumberWidth,
		CashFlowCategory cashFlowCategory) : base(accountName, accountNumber, cashFlowCategory,
		CreateAccountNumberSpace(8, accountNumberWidth)) { }
}

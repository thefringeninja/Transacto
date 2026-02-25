namespace Transacto.Domain;

public record OtherIncomeAccount : IncomeAccount {
	public OtherIncomeAccount(AccountName accountName, AccountNumber accountNumber, AccountNumberWidth accountNumberWidth,
		CashFlowCategory cashFlowCategory) : base(accountName, accountNumber, cashFlowCategory,
		CreateAccountNumberSpace(7, accountNumberWidth)) {
	}
}

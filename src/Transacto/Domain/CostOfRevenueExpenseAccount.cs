namespace Transacto.Domain;

public record CostOfRevenueExpenseAccount : ExpenseAccount {
	public CostOfRevenueExpenseAccount(AccountName accountName, AccountNumber accountNumber, int accountNumberWidth) :
		base(accountName, accountNumber, CreateAccountNumberSpace(5, accountNumberWidth)) {
	}
}
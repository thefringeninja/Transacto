namespace Transacto.Domain;

public record OperatingExpenseAccount : ExpenseAccount {
	public OperatingExpenseAccount(AccountName accountName, AccountNumber accountNumber, int accountNumberWidth) : 
		base(accountName, accountNumber, CreateAccountNumberSpace(6, accountNumberWidth)) {
	}
}
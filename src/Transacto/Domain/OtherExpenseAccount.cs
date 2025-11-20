namespace Transacto.Domain;

public record OtherExpenseAccount : ExpenseAccount {
	public OtherExpenseAccount(AccountName accountName, AccountNumber accountNumber, int accountNumberWidth) :
		base(accountName, accountNumber, CreateAccountNumberSpace(8, accountNumberWidth)) {
	}
}
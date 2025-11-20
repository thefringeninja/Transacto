namespace Transacto.Domain;

public record OtherIncomeAccount : IncomeAccount {
	public OtherIncomeAccount(AccountName accountName, AccountNumber accountNumber, int accountNumberWidth)
		: base(accountName, accountNumber, CreateAccountNumberSpace(7, accountNumberWidth)) {
	}
}

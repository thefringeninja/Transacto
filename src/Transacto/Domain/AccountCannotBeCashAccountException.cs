namespace Transacto.Domain;

public class AccountCannotBeCashAccountException(AccountNumber accountNumber)
	: Exception($"Account {accountNumber} cannot be a cash account") {
	public AccountNumber AccountNumber { get; } = accountNumber;
}

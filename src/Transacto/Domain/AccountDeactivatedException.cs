namespace Transacto.Domain;

public class AccountDeactivatedException(AccountNumber accountNumber)
	: Exception($"Account {accountNumber} was deactivated.") {
	public AccountNumber AccountNumber { get; } = accountNumber;
}

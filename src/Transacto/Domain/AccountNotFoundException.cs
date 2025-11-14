namespace Transacto.Domain;

public class AccountNotFoundException(AccountNumber accountNumber)
	: Exception($"Account {accountNumber} was not found.") {
	public AccountNumber AccountNumber { get; } = accountNumber;
}

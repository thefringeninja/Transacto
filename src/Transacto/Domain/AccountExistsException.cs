namespace Transacto.Domain;

public class AccountExistsException(AccountNumber accountNumber)
	: Exception($"Account {accountNumber} already exists.") {
	public AccountNumber AccountNumber { get; } = accountNumber;
}

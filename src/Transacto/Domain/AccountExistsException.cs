namespace Transacto.Domain;

public class AccountExistsException : Exception {
	public AccountNumber AccountNumber { get; }

	public AccountExistsException(AccountNumber accountNumber) : base($"Account {accountNumber} already exists.") {
		AccountNumber = accountNumber;
	}
}

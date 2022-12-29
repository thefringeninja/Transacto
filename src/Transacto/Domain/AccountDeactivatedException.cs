namespace Transacto.Domain;

public class AccountDeactivatedException : Exception {
	public AccountNumber AccountNumber { get; }

	public AccountDeactivatedException(AccountNumber accountNumber) : base(
		$"Account {accountNumber} was deactivated.") {
		AccountNumber = accountNumber;
	}
}

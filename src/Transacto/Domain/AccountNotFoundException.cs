using System;

namespace Transacto.Domain; 

public class AccountNotFoundException : Exception {
	public AccountNumber AccountNumber { get; }

	public AccountNotFoundException(AccountNumber accountNumber) : base($"Account {accountNumber} was not found.") {
		AccountNumber = accountNumber;
	}
}
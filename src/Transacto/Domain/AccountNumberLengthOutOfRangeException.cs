namespace Transacto.Domain;

public class AccountNumberLengthOutOfRangeException(AccountNumber accountNumber, AccountNumberWidth expectedLength, string paramName)
	: ArgumentOutOfRangeException($"Excpected account number {accountNumber} to have length {expectedLength}",
		paramName) {
	public AccountNumber AccountNumber { get; } = accountNumber;

	public AccountNumberWidth ExpectedLength { get; } = expectedLength;
}

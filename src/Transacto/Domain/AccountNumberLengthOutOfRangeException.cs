namespace Transacto.Domain;

public class AccountNumberLengthOutOfRangeException(AccountNumber accountNumber, int expectedLength, string paramName)
	: ArgumentOutOfRangeException($"Excpected account number {accountNumber} to have length {expectedLength}",
		paramName) {
	public AccountNumber AccountNumber { get; } = accountNumber;

	public int ExpectedLength { get; } = expectedLength;
}

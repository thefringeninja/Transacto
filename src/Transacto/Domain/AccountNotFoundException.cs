using Transacto.Messages;

namespace Transacto.Domain;

public class AccountNotFoundException(AccountNumber accountNumber)
	: Exception(FormatMessage(accountNumber).ToString()), IFormattableMessage {
	private static FormattableString FormatMessage(AccountNumber accountNumber) =>
		$"Account {accountNumber.ToInt32()} was not found.";

	public AccountNumber AccountNumber { get; } = accountNumber;
	public FormattableString ToFormattableString() => FormatMessage(AccountNumber);
}

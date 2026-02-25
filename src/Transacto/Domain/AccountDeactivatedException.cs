using Transacto.Messages;

namespace Transacto.Domain;

public class AccountDeactivatedException(AccountNumber accountNumber)
	: Exception(FormatMessage(accountNumber).ToString()), IFormattableMessage {
	private static FormattableString FormatMessage(AccountNumber accountNumber) {
		return $"Account {accountNumber} was deactivated.";
	}

	public AccountNumber AccountNumber { get; } = accountNumber;
	public FormattableString ToFormattableString() => FormatMessage(AccountNumber);
}

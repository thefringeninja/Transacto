using Transacto.Messages;

namespace Transacto.Domain;

public class AccountExistsException(AccountNumber accountNumber)
	: Exception(FormatMessage(accountNumber).ToString()), IFormattableMessage {
	private static FormattableString FormatMessage(AccountNumber accountNumber) =>
		$"Account {accountNumber.ToInt32()} already exists.";

	public AccountNumber AccountNumber { get; } = accountNumber;
	public FormattableString ToFormattableString() => FormatMessage(AccountNumber);
}

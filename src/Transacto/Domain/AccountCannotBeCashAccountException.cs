using Transacto.Messages;

namespace Transacto.Domain;

public class AccountCannotBeCashAccountException(AccountNumber accountNumber)
	: Exception(FormatMessage(accountNumber).ToString()), IFormattableMessage {
	private static FormattableString FormatMessage(AccountNumber accountNumber)
	{
		return $"Account {accountNumber} cannot be a cash account";
	}

	public AccountNumber AccountNumber { get; } = accountNumber;
	public FormattableString ToFormattableString() => FormatMessage(AccountNumber);
}

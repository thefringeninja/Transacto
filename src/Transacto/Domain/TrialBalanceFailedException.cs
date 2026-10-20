using Transacto.Messages;

namespace Transacto.Domain;

public class TrialBalanceFailedException(Money balance)
	: Exception(FormatMessage(balance).ToString()), IFormattableMessage {
	private static FormattableString FormatMessage(Money balance) =>
		$"Expected a balance of {Money.Zero}, current trial balance is {balance}.";

	public Money Balance { get; } = balance;

	public FormattableString ToFormattableString() => FormatMessage(Balance);
}

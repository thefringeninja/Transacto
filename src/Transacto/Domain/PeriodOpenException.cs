using Transacto.Messages;

namespace Transacto.Domain;

public class PeriodOpenException(AccountingPeriod accountingPeriod)
	: Exception(FormatMessage(accountingPeriod).ToString()), IFormattableMessage {
	public AccountingPeriod AccountingPeriod { get; } = accountingPeriod;

	private static FormattableString FormatMessage(AccountingPeriod accountingPeriod) =>
		$"Period {accountingPeriod.ToString()} is still open.";

	public FormattableString ToFormattableString() => FormatMessage(AccountingPeriod);
}

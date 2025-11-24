using Transacto.Messages;

namespace Transacto.Domain;

public class PeriodClosingInProcessException(AccountingPeriod accountingPeriod)
	: Exception(FormatMessage(accountingPeriod).ToString()), IFormattableMessage {
	private static FormattableString FormatMessage(AccountingPeriod accountingPeriod) =>
		$"Closing period {accountingPeriod.ToString()} is already in process.";

	public FormattableString ToFormattableString() => FormatMessage(accountingPeriod);
}

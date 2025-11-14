namespace Transacto.Domain;

public class PeriodOpenException(AccountingPeriod accountingPeriod)
	: Exception($"Period {accountingPeriod} is still open.") {
	public AccountingPeriod AccountingPeriod { get; } = accountingPeriod;
}

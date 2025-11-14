namespace Transacto.Domain;

public class PeriodClosingInProcessException(AccountingPeriod accountingPeriod)
	: Exception($"Closing period {accountingPeriod} is already in process.");

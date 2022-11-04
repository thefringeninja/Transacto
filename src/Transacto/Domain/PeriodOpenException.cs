using System;

namespace Transacto.Domain; 

public class PeriodOpenException : Exception {
	public AccountingPeriod AccountingPeriod { get; }

	public PeriodOpenException(AccountingPeriod accountingPeriod) : base($"Period {accountingPeriod} is still open.") {
		AccountingPeriod = accountingPeriod;
	}
}
using System;

namespace Transacto.Domain; 

public class PeriodClosingInProcessException : Exception {
	public PeriodClosingInProcessException(AccountingPeriod accountingPeriod) :
		base($"Closing period {accountingPeriod} is already in process.") {
	}
}
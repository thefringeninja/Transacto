using System;

namespace Transacto.Domain {
	public class PeriodClosingInProcessException : Exception {
		public PeriodClosingInProcessException(Period period) :
			base($"Closing period {period} is already in process.") {
		}
	}
}

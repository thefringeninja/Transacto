using System;

namespace Transacto.Domain {
	public class PeriodOpenException : Exception {
		public Period Period { get; }

		public PeriodOpenException(Period period) : base($"Period {period} is still open.") {
			Period = period;
		}
	}
}

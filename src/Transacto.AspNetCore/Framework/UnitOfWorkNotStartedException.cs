using System;

namespace Transacto.Framework {
	public class UnitOfWorkNotStartedException : Exception {
		public UnitOfWorkNotStartedException() : base("The UnitOfWork has not started.") {
		}
	}
}

using System;

namespace Transacto.Framework.CommandHandling {
	public class UnitOfWorkNotStartedException : Exception {
		public UnitOfWorkNotStartedException() : base("The UnitOfWork has not started.") {
		}
	}
}

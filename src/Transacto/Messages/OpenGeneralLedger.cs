using System;

namespace Transacto.Messages {
	public class OpenGeneralLedger {
		public DateTimeOffset OpenedOn { get; set; }

		public override string ToString() => $"Opening general ledger on {OpenedOn:O}.";
	}
}

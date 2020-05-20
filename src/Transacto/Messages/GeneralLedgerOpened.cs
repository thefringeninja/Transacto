using System;

namespace Transacto.Messages {
	public class GeneralLedgerOpened {
		public DateTimeOffset OpenedOn { get; set; }
		public override string ToString() => $"The general ledger was opened on {OpenedOn:O}.";
	}
}

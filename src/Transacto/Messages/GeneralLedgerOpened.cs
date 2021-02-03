using System;

namespace Transacto.Messages {
	public record GeneralLedgerOpened {
		public DateTimeOffset OpenedOn;

		public override string ToString() => $"The general ledger was opened on {OpenedOn:O}.";
	}
}

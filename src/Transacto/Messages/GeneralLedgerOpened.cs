using System;

namespace Transacto.Messages {
	public record GeneralLedgerOpened {
		public DateTimeOffset OpenedOn { get; init; }

		public override string ToString() => $"The general ledger was opened on {OpenedOn:O}.";
	}
}

using System;

namespace Transacto.Messages {
	public record GeneralLedgerEntryPosted {
		public Guid GeneralLedgerEntryId;
		public string Period = default!;

		public override string ToString() => "General ledger entry was posted.";
	}
}

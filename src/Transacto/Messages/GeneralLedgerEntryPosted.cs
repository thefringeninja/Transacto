using System;

namespace Transacto.Messages {
	public record GeneralLedgerEntryPosted {
		public Guid GeneralLedgerEntryId;
		public string Period { get; init; } = default!;

		public override string ToString() => "General ledger entry was posted.";
	}
}

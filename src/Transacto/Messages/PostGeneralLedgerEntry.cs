using System;
using Transacto.Domain;

namespace Transacto.Messages {
	public class PostGeneralLedgerEntry {
		public Guid GeneralLedgerEntryId { get; set; }
		public string Period { get; set; } = null!;
		public DateTimeOffset CreatedOn { get; set; }
		public IBusinessTransaction BusinessTransaction { get; set; } = null!;

		public override string ToString() =>
			$"Posting general ledger entry {BusinessTransaction.ReferenceNumber} in period {Period} on {CreatedOn:O}.";
	}
}

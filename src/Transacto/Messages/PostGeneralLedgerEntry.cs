using System.Text.Json.Serialization;
using Transacto.Domain;

namespace Transacto.Messages {
	partial class PostGeneralLedgerEntry {
		public PostGeneralLedgerEntry() {
			Period = null!;
		}
		[JsonPropertyName("document")]
		public IBusinessTransaction? BusinessTransaction { get; set; }

		public override string ToString() =>
			$"Posting general ledger entry {BusinessTransaction?.ReferenceNumber.ToString() ?? "unknown"} in period {Period} on {CreatedOn:O}.";
	}
}

using System.Text.Json.Serialization;
using Transacto.Domain;

namespace Transacto.Messages {
	public partial record PostGeneralLedgerEntry {
		public PostGeneralLedgerEntry() {
			Period = null!;
		}

		[JsonPropertyName("document")] public IBusinessTransaction? BusinessTransaction;

		public override string ToString() =>
			$"Posting general ledger entry {BusinessTransaction?.ReferenceNumber.ToString() ?? "unknown"} in period {Period} on {CreatedOn:O}.";
	}
}

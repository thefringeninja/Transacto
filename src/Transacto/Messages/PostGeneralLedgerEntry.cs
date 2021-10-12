using System.Text.Json.Serialization;
using Transacto.Domain;

namespace Transacto.Messages {
	public partial record PostGeneralLedgerEntry {
		public PostGeneralLedgerEntry() {
			Period = null!;
		}

		[JsonPropertyName("document")] public IBusinessTransaction? BusinessTransaction { get; init; }

		public override string ToString() =>
			$"Posting general ledger entry {BusinessTransaction?.SequenceNumber.ToString() ?? "unknown"} in period {Period} on {CreatedOn:O}.";
	}
}

using System.Text.Json.Serialization;
using Transacto.Domain;

namespace Transacto.Messages;

public partial record PostGeneralLedgerEntry : IFormattableMessage {
	[JsonPropertyName("document")] public required IBusinessTransaction BusinessTransaction { get; set; }

	public FormattableString ToFormattableString() => 
		$"Posting general ledger entry {GeneralLedgerEntryId} in accounting period {Period} on {CreatedOn:yyyy'-'MM'-'dd}"; 
}

using System.Text.Json.Serialization;
using Transacto.Domain;

namespace Transacto.Messages;

public partial record PostGeneralLedgerEntry : IFormattableMessage {
	[JsonPropertyName("document"), JsonIgnore] public required IBusinessTransaction BusinessTransaction { get; init; }

	public FormattableString ToFormattableString() => 
		$"Posting general ledger entry {GeneralLedgerEntryId} in accounting period {Period} on {CreatedOn:yyyy'-'MM'-'dd}"; 
}

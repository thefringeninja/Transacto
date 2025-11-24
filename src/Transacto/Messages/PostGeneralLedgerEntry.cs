using System.Text.Json.Serialization;
using Transacto.Domain;

namespace Transacto.Messages;

public partial record PostGeneralLedgerEntry : IFormattableMessage {
	[JsonPropertyName("document")] public required IBusinessTransaction BusinessTransaction { get; init; }

	public FormattableString ToFormattableString() => 
		$"Posting general ledger entry {BusinessTransaction.SequenceNumber} in accounting period {Period} on {CreatedOn:O}"; 
}

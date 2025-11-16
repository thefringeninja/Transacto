using System.Text.Json.Serialization;
using Transacto.Domain;

namespace Transacto.Messages;

public partial record PostGeneralLedgerEntry : IMessage {
	[JsonPropertyName("document")] public required IBusinessTransaction BusinessTransaction { get; init; }

	public (string template, object[] args) GetMessage() => (
		"Posting general ledger entry {SequenceNumber} in period {Period} on {CreatedOn:O}",
		[BusinessTransaction.SequenceNumber, Period, CreatedOn]); 
}

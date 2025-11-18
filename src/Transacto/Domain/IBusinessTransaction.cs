using System.Text.Json.Serialization;

namespace Transacto.Domain;

public interface IBusinessTransaction {
	[JsonIgnore] public GeneralLedgerEntrySequenceNumber SequenceNumber { get; }
	public IEnumerable<object> GetTransactionItems();
}

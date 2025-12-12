using System.Text.Json.Serialization;

namespace Transacto.Domain;

public interface IBusinessTransaction {
	public IEnumerable<object> GetTransactionItems();
	
	public GeneralLedgerEntrySequenceNumber? SequenceNumber { get; }
	
	public static abstract GeneralLedgerEntryNumberPrefix Prefix { get; }

	public GeneralLedgerEntryNumber? ReferenceNumber { get; }
}

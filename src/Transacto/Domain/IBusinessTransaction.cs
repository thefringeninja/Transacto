namespace Transacto.Domain;

public interface IBusinessTransaction {
	public GeneralLedgerEntrySequenceNumber SequenceNumber { get; }
	public IEnumerable<object> GetTransactionItems();
}

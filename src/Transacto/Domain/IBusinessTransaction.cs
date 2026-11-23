using System.Text.Json.Serialization;

namespace Transacto.Domain;

public interface IBusinessTransaction {
	public IEnumerable<TransactionItem> GetTransactionItems(ChartOfAccounts chartOfAccounts);
	
	public GeneralLedgerEntrySequenceNumber? SequenceNumber { get; set; }
	
	public static virtual GeneralLedgerEntryNumberPrefix Prefix => throw new NotImplementedException();

	public GeneralLedgerEntryNumber? ReferenceNumber { get; }
}

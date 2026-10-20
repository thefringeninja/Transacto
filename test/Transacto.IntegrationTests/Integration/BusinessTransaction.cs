using System.Runtime.Serialization;
using Transacto.Domain;

namespace Transacto.Integration;

[DataContract]
internal class BusinessTransaction : IBusinessTransaction {
	[DataMember(Name = "transactionId")] public Guid TransactionId { get; set; }
	public GeneralLedgerEntrySequenceNumber? SequenceNumber { get; set; }
	public static GeneralLedgerEntryNumberPrefix Prefix { get; } = new("transaction");
	public GeneralLedgerEntryNumber? ReferenceNumber =>
		SequenceNumber.HasValue ? new GeneralLedgerEntryNumber(Prefix, SequenceNumber.Value) : null;

	public IEnumerable<object> GetTransactionItems(ChartOfAccounts chartOfAccounts) {
		yield return new Debit(new AccountNumber(1000), new Money(5m));
		yield return new Credit(new AccountNumber(3000), new Money(5m));
	}
}

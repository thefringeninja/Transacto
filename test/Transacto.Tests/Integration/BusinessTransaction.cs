using System.Runtime.Serialization;
using Transacto.Domain;

namespace Transacto.Integration;

[DataContract]
internal class BusinessTransaction : IBusinessTransaction {
	[DataMember(Name = "transactionId")] public Guid TransactionId { get; set; }
	[DataMember(Name = "referenceNumber")] public int TransactionNumber { get; set; }
	public GeneralLedgerEntrySequenceNumber SequenceNumber => new(TransactionNumber);

	public IEnumerable<object> GetTransactionItems() {
		yield return new Debit(new AccountNumber(1000), new Money(5m));
		yield return new Credit(new AccountNumber(3000), new Money(5m));
	}
}

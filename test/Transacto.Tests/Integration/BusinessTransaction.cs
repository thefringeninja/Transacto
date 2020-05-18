using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Transacto.Domain;

namespace Transacto.Integration {
	[DataContract]
	internal class BusinessTransaction : IBusinessTransaction {
		[DataMember(Name = "transactionId")] public Guid TransactionId { get; set; }
		[DataMember(Name = "referenceNumber")] public int ReferenceNumber { get; set; }

		GeneralLedgerEntryNumber IBusinessTransaction.ReferenceNumber =>
			new GeneralLedgerEntryNumber($"t-{ReferenceNumber}");

		public void Apply(GeneralLedgerEntry entry) {
			entry.ApplyDebit(new Debit(new AccountNumber(1000), new Money(5m)));
			entry.ApplyCredit(new Credit(new AccountNumber(3000), new Money(5m)));
			entry.ApplyTransaction(this);
		}

		public IEnumerable<object> GetAdditionalChanges() {
			yield return this;
		}

		public int? Version { get; set; }
	}
}

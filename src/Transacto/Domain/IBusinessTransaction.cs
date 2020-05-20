using System.Collections.Generic;

namespace Transacto.Domain {
	public interface IBusinessTransaction {
		public GeneralLedgerEntryNumber ReferenceNumber { get; }
		public void Apply(GeneralLedgerEntry generalLedgerEntry, AccountIsDeactivated accountIsDeactivated);
		IEnumerable<object> GetAdditionalChanges();
		int? Version { get; set; }
	}
}

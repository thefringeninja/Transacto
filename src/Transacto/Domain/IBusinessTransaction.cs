using System.Collections.Generic;
using Transacto.Framework;

namespace Transacto.Domain {
	public interface IBusinessTransaction {
		public GeneralLedgerEntryNumber ReferenceNumber { get; }
		public void Apply(GeneralLedgerEntry generalLedgerEntry, ChartOfAccounts chartOfAccounts);
		IEnumerable<object> GetAdditionalChanges();
		int? Version { get; set; }
	}

	public static class BusinessTransactionExtensions {
		public static T WithVersion<T>(this T source, Optional<int> version) where T : IBusinessTransaction {
			source.Version = version.HasValue ? version.Value : new int?();
			return source;
		}
	}
}

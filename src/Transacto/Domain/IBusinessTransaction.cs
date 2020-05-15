using System;
using System.Collections.Generic;
using Transacto.Framework;

namespace Transacto.Domain {
	public interface IBusinessTransaction {
		public GeneralLedgerEntry GetGeneralLedgerEntry(PeriodIdentifier period, DateTimeOffset createdOn);
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

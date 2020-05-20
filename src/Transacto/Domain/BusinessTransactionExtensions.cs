using Transacto.Framework;

namespace Transacto.Domain {
	public static class BusinessTransactionExtensions {
		public static T WithVersion<T>(this T source, Optional<int> version) where T : IBusinessTransaction {
			source.Version = version.HasValue ? version.Value : new int?();
			return source;
		}
	}
}

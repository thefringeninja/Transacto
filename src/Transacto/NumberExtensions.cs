using System.Numerics;

namespace Transacto;

internal static class NumberExtensions {
	public static T Sum<T>(this IEnumerable<T> values) where T : IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T> =>
		values.Aggregate(T.AdditiveIdentity, (a, b) => a + b);

}

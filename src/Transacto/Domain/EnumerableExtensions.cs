namespace Transacto.Domain; 

internal static class EnumerableExtensions {
	public static Money Sum(this IEnumerable<Money> source) => source.Aggregate(Money.Zero, Add);

	private static Money Add(Money current, Money amount) => current + amount;
}

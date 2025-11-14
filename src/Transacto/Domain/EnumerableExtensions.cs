namespace Transacto.Domain;

internal static class EnumerableExtensions {
	public static Money Sum(this IEnumerable<Debit> source) => source.Aggregate(Money.Zero, Add);

	private static Money Add(Money acc, Debit source) => acc + source.Amount;
	
	public static Money Sum(this IEnumerable<Credit> source) => source.Aggregate(Money.Zero, Add);

	private static Money Add(Money acc, Credit source) => acc + source.Amount;
}

// ReSharper disable CheckNamespace

namespace System.Collections.Immutable {
	// ReSharper restore CheckNamespace

	public static class ImmutableExtensions {
		public static ImmutableDictionary<TKey, TValue> Compact<TKey, TValue>(
			this ImmutableDictionary<TKey, TValue> source) where TKey : notnull {
			var builder = ImmutableDictionary<TKey, TValue>.Empty.ToBuilder();
			builder.AddRange(source);
			return builder.ToImmutable();
		}

		public static ImmutableHashSet<T> Compact<T>(this ImmutableHashSet<T> source) {
			var builder = ImmutableHashSet<T>.Empty.ToBuilder();
			builder.UnionWith(source);
			return builder.ToImmutable();
		}
	}
}

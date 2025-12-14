// ReSharper disable once CheckNamespace

namespace KurrentDB.Client;

using Transacto.Framework;

internal static class KurrentExtensions {
	extension(StreamState value) {
		public StreamState Next() {
			ArgumentOutOfRangeException.ThrowIfEqual(value.HasPosition, false);
			return StreamState.StreamRevision((ulong)(value.ToInt64() + 1));
		}
	}

	extension(EventTypeFilter) {
		public static IEventFilter OfType<TEventType>(IMessageTypeMapper mapper) =>
			!mapper.TryMap(typeof(TEventType), out var storageType)
				? throw new ArgumentOutOfRangeException(nameof(TEventType))
				: EventTypeFilter.RegularExpression($"^{storageType}$");
	}
}

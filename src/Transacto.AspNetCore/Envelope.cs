using System;
using System.Collections.Concurrent;
using System.Runtime.Intrinsics.Arm;
using EventStore.Client;

namespace Transacto {
	public record Envelope {
		private static readonly ConcurrentDictionary<Type, Func<object, Position, Envelope>> Cache =
			new();

		public static Envelope Create(object message, Position position) =>
			Cache.GetOrAdd(message.GetType(), _ => (m, p) => (Envelope)typeof(Envelope<>)
					.MakeGenericType(m.GetType())
					.GetConstructor(new[] {m.GetType(), typeof(Position)})!.Invoke(new[] {m, p}))
				.Invoke(message, position);
	}

	public record Envelope<T>(T Message, Position Position) : Envelope;
}

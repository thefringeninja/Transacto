using System;
using System.Collections.Concurrent;
using EventStore.Client;

namespace Transacto {
	public abstract class Envelope {
		private static readonly ConcurrentDictionary<Type, Func<object, Position, Envelope>> Cache =
			new ConcurrentDictionary<Type, Func<object, Position, Envelope>>();

		public object Message { get; }
		public Position Position { get; }

		public static Envelope Create(object message, Position position) =>
			Cache.GetOrAdd(message.GetType(), type => (m, p) => (Envelope)typeof(Envelope<>)
					.MakeGenericType(m.GetType())
					.GetConstructor(new[] {m.GetType(), typeof(Position)})!.Invoke(new[] {m, p}))
				.Invoke(message, position);

		protected Envelope(object message, Position position) {
			Message = message;
			Position = position;
		}
	}

	public class Envelope<T> : Envelope {
		public new T Message { get; }

		public Envelope(T message, Position position) : base(message!, position) {
			Message = message;
		}
	}
}

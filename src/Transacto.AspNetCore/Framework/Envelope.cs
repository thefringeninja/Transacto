using EventStore.Client;
using Projac;

namespace Transacto.Framework; 

public static class EnvelopeResolve {
	public static ProjectionHandlerResolver<TConnection> WhenAssignableToHandlerMessageType<TConnection>(
		ProjectionHandler<TConnection>[] handlers) =>
		handlers switch {
			null => throw new ArgumentNullException(nameof(handlers)),
			_ => message => message switch {
				null => throw new ArgumentNullException(nameof(message)),
				_ => handlers
			}
		};
}

public record Envelope(object Message, Position Position);

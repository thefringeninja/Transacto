namespace Transacto.Framework.ProcessManagers; 

public static class ProcessManagerEventResolve {
	public static MessageHandlerResolver<Checkpoint> WhenEqualToHandlerMessageType(
		IEnumerable<MessageHandler<Checkpoint>> handlers) {
		var cache = handlers.ToLookup(h => h.Message).ToDictionary(x => x.Key, x => x.ToArray());

		return @event => {
			var type = @event.GetType();

			cache.TryGetValue(type, out var handlers);

			return handlers switch {
				null or {Length: 0} => new MessageHandler<Checkpoint>(type,
					(_, _) => new ValueTask<Checkpoint>(Checkpoint.None)),
				{Length: 1} => handlers[0],
				_ => throw new ProcessManagerEventResolveException(type, handlers.Length)
			};
		};
	}
}

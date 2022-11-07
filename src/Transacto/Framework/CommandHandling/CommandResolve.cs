namespace Transacto.Framework.CommandHandling; 

public static class CommandResolve {
	public static MessageHandlerResolver<Checkpoint> WhenEqualToHandlerMessageType(
		IEnumerable<MessageHandler<Checkpoint>> handlers) {
		var cache = (from handler in handlers
			group handler by handler.Message
			into g
			select g).ToDictionary(x => x.Key, x => x.ToArray());

		return command => {
			var type = command.GetType();

			cache.TryGetValue(type, out var handlers);

			return handlers switch {
				{Length: 1} => handlers[0],
				_ => throw new CommandResolveException(type, handlers?.Length ?? 0)
			};
		};
	}

	public static MessageHandlerResolver<Checkpoint> WhenEqualToHandlerMessageType(
		IEnumerable<CommandHandlerModule> modules) =>
		WhenEqualToHandlerMessageType(modules.SelectMany(m => m));
}

namespace Transacto.Framework.CommandHandling;

public class CommandDispatcher {
	private readonly MessageHandlerResolver<Checkpoint> _resolver;

	public CommandDispatcher(IEnumerable<CommandHandlerModule> commandHandlerModules)
		: this(CommandResolve.WhenEqualToHandlerMessageType(commandHandlerModules)) {
	}

	public CommandDispatcher(MessageHandlerResolver<Checkpoint> resolver) {
		_resolver = resolver;
	}

	public ValueTask<Checkpoint> Handle(object command, CancellationToken cancellationToken = default) =>
		_resolver.Invoke(command).Handler(command, cancellationToken);
}

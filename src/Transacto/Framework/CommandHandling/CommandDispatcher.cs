namespace Transacto.Framework.CommandHandling;

public class CommandDispatcher(MessageHandlerResolver<Checkpoint> resolver) {
	public CommandDispatcher(IEnumerable<CommandHandlerModule> commandHandlerModules)
		: this(CommandResolve.WhenEqualToHandlerMessageType(commandHandlerModules)) {
	}

	public ValueTask<Checkpoint> Handle(object command, CancellationToken cancellationToken = default) =>
		resolver.Invoke(command).Handler(command, cancellationToken);
}

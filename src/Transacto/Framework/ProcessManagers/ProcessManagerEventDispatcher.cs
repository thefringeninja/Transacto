namespace Transacto.Framework.ProcessManagers;

public class ProcessManagerEventDispatcher(MessageHandlerResolver<Checkpoint> resolver) {
	public ProcessManagerEventDispatcher(ProcessManagerEventHandlerModule eventHandlerModule)
		: this(ProcessManagerEventResolve.WhenEqualToHandlerMessageType(eventHandlerModule)) {
	}

	public ValueTask<Checkpoint> Handle(object @event, CancellationToken cancellationToken = default) =>
		resolver.Invoke(@event).Handler(@event, cancellationToken);
}

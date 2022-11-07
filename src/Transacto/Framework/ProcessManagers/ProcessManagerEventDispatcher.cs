namespace Transacto.Framework.ProcessManagers; 

public class ProcessManagerEventDispatcher {
	private readonly MessageHandlerResolver<Checkpoint> _resolver;

	public ProcessManagerEventDispatcher(ProcessManagerEventHandlerModule eventHandlerModule)
		: this(ProcessManagerEventResolve.WhenEqualToHandlerMessageType(eventHandlerModule)) {
	}

	public ProcessManagerEventDispatcher(MessageHandlerResolver<Checkpoint> resolver) {
		_resolver = resolver;
	}

	public ValueTask<Checkpoint> Handle(object @event, CancellationToken cancellationToken = default) =>
		_resolver.Invoke(@event).Handler(@event, cancellationToken);
}

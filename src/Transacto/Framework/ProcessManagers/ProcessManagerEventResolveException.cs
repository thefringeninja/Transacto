namespace Transacto.Framework.ProcessManagers;

public class ProcessManagerEventResolveException(Type eventType, int handlerCount)
	: Exception($"Expected one registration for event {eventType}, {handlerCount} found.") {
	public Type EventType { get; } = eventType;
	public int HandlerCount { get; } = handlerCount;
}

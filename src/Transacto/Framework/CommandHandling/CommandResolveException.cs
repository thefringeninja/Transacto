namespace Transacto.Framework.CommandHandling;

public class CommandResolveException : Exception {
	public Type CommandType { get; }
	public int HandlerCount { get; }

	public CommandResolveException(Type commandType, int handlerCount)
		: base($"Expected one registration for command {commandType}, {handlerCount} found.") {
		CommandType = commandType;
		HandlerCount = handlerCount;
	}
}

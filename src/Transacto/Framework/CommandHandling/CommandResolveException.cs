namespace Transacto.Framework.CommandHandling;

public class CommandResolveException(Type commandType, int handlerCount)
	: Exception($"Expected one registration for command {commandType}, {handlerCount} found.") {
	public Type CommandType { get; } = commandType;
	public int HandlerCount { get; } = handlerCount;
}

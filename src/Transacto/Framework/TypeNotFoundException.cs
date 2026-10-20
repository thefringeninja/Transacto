namespace Transacto.Framework;

public class TypeNotFoundException(Type type, Exception? innerException = null)
	: Exception($"No storage type for '{type}' was found.", innerException) {
	public Type Type { get; } = type;
}

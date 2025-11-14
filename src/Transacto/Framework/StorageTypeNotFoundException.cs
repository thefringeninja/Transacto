namespace Transacto.Framework;

public class StorageTypeNotFoundException(string storageType, Exception? innerException = null)
	: Exception($"No type for '{storageType}' was found.", innerException) {
	public string StorageType { get; } = storageType;
}

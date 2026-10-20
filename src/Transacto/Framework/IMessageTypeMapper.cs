using System.Diagnostics.CodeAnalysis;

namespace Transacto.Framework;

public interface IMessageTypeMapper {
	string Map(Type type) => !TryMap(type, out var t) ? throw new TypeNotFoundException(type) : t!;

	Type Map(string storageType) =>
		!TryMap(storageType, out var t) ? throw new StorageTypeNotFoundException(storageType) : t!;

	bool TryMap(string storageType, [NotNullWhen(true)] out Type? type);
	bool TryMap(Type type, [NotNullWhen(true)] out string? storageType);

	IEnumerable<string> StorageTypes { get; }
	IEnumerable<Type> Types { get; }
}

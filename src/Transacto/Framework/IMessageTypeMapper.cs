using System;
using System.Collections.Generic;

namespace Transacto.Framework {
    public interface IMessageTypeMapper {
	    string Map(Type type) => !TryMap(type, out var t) ? throw new InvalidOperationException() : t!;
	    Type Map(string storageType) => !TryMap(storageType, out var t) ? throw new InvalidOperationException() : t!;

        bool TryMap(string storageType, out Type? type);
        bool TryMap(Type type, out string? storageType);

        IEnumerable<string> StorageTypes { get; }
        IEnumerable<Type> Types { get; }
    }
}

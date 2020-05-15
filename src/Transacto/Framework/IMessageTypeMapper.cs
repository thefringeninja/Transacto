using System;
using System.Collections.Generic;

namespace Transacto.Framework {
    public interface IMessageTypeMapper {
        string? Map(Type type);
        Type? Map(string storageType);
        IEnumerable<string> StorageTypes { get; }
        IEnumerable<Type> Types { get; }
    }
}

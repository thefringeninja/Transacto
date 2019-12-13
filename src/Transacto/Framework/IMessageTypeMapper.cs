using System;

namespace Transacto.Framework {
    public interface IMessageTypeMapper {
        string? Map(Type type);
        Type? Map(string storageType);
    }
}

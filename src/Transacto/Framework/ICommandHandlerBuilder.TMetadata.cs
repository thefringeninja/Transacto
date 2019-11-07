using System;
using System.Threading;
using System.Threading.Tasks;

namespace Transacto.Framework {
    public interface ICommandHandlerBuilder<TCommand, TMetadata> {
        ICommandHandlerBuilder<TCommand, TMetadata> Pipe(
            Func<Func<TCommand, TMetadata, CancellationToken, ValueTask>, Func<TCommand, TMetadata, CancellationToken, ValueTask>>
                pipe);

        ICommandHandlerBuilder<TNext, TMetadata> Transform<TNext>(
            Func<Func<TNext, TMetadata, CancellationToken, ValueTask>, Func<TCommand, TMetadata, CancellationToken, ValueTask>>
                pipe);

        void Handle(Func<TCommand, TMetadata, CancellationToken, ValueTask> handler);
    }
}

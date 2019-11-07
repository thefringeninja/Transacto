using System;
using System.Threading;
using System.Threading.Tasks;

namespace Transacto.Framework {
    public interface ICommandHandlerBuilder<TCommand> {
        ICommandHandlerBuilder<TCommand> Pipe(
            Func<Func<TCommand, CancellationToken, ValueTask>, Func<TCommand, CancellationToken, ValueTask>> pipe);

        ICommandHandlerBuilder<TNext> Transform<TNext>(
            Func<Func<TNext, CancellationToken, ValueTask>, Func<TCommand, CancellationToken, ValueTask>> pipe);

        void Handle(Func<TCommand, CancellationToken, ValueTask> handler);
    }
}

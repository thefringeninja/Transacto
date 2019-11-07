using System;
using System.Threading;
using System.Threading.Tasks;

namespace Transacto.Framework {
    public class CommandHandler<TMetadata> {
        public CommandHandler(Type command, Func<object, TMetadata, CancellationToken, ValueTask> handler) {
            Command = command ?? throw new ArgumentNullException(nameof(command));
            Handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        public Type Command { get; }
        public Func<object, TMetadata, CancellationToken, ValueTask> Handler { get; }

        public CommandHandler<TMetadata> Pipe(
            Func<Func<object, TMetadata, CancellationToken, ValueTask>, Func<object, TMetadata, CancellationToken, ValueTask>>
                pipe) {
            if (pipe == null) {
                throw new ArgumentNullException(nameof(pipe));
            }

            return new CommandHandler<TMetadata>(Command, pipe(Handler));
        }
    }
}

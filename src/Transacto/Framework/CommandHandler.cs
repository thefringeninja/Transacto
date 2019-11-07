using System;
using System.Threading;
using System.Threading.Tasks;

namespace Transacto.Framework {
    public class CommandHandler {
        public CommandHandler(Type command, Func<object, CancellationToken, ValueTask> handler) {
            Command = command ?? throw new ArgumentNullException(nameof(command));
            Handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        public Type Command { get; }
        public Func<object, CancellationToken, ValueTask> Handler { get; }

        public CommandHandler Pipe(
            Func<Func<object, CancellationToken, ValueTask>, Func<object, CancellationToken, ValueTask>> pipe) {
            if (pipe == null) {
                throw new ArgumentNullException(nameof(pipe));
            }

            return new CommandHandler(Command, pipe(Handler));
        }
    }
}

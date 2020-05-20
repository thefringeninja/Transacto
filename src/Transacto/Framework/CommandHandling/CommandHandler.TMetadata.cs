using System;
using System.Threading;
using System.Threading.Tasks;

namespace Transacto.Framework.CommandHandling {
	public class CommandHandler<TMetadata> {
		public CommandHandler(Type command, Func<object, TMetadata, CancellationToken, ValueTask> handler) {
			Command = command;
			Handler = handler;
		}

		public Type Command { get; }
		public Func<object, TMetadata, CancellationToken, ValueTask> Handler { get; }

		public CommandHandler<TMetadata> Pipe(Func<
			Func<object, TMetadata, CancellationToken, ValueTask>,
			Func<object, TMetadata, CancellationToken, ValueTask>> pipe) =>
			new CommandHandler<TMetadata>(Command, pipe(Handler));
	}
}

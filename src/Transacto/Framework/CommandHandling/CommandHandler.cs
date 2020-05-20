using System;
using System.Threading;
using System.Threading.Tasks;
using EventStore.Client;

namespace Transacto.Framework.CommandHandling {
	public class CommandHandler {
		public CommandHandler(Type command, Func<object, CancellationToken, ValueTask<Position>> handler) {
			Command = command;
			Handler = handler;
		}

		public Type Command { get; }
		public Func<object, CancellationToken, ValueTask<Position>> Handler { get; }

		public CommandHandler Pipe(Func<
			Func<object, CancellationToken, ValueTask<Position>>,
			Func<object, CancellationToken, ValueTask<Position>>> pipe) => new CommandHandler(Command, pipe(Handler));
	}
}

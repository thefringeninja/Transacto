using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EventStore.Client;
using Transacto.Framework.CommandHandling;

namespace Transacto.Framework {
	public class CommandDispatcher {
		private readonly CommandHandlerResolver _resolver;

		public CommandDispatcher(IEnumerable<CommandHandlerModule> commandHandlerModules) {
			_resolver = CommandResolve.WhenEqualToHandlerMessageType(commandHandlerModules);
		}

		public ValueTask<Position> Handle(object command, CancellationToken cancellationToken = default) =>
			_resolver.Invoke(command).Handler(command, cancellationToken);
	}
}

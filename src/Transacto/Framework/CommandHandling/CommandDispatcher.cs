using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EventStore.Client;

namespace Transacto.Framework.CommandHandling {
	public class CommandDispatcher {
		private readonly MessageHandlerResolver<Position> _resolver;

		public CommandDispatcher(IEnumerable<CommandHandlerModule> commandHandlerModules) {
			_resolver = CommandResolve.WhenEqualToHandlerMessageType(commandHandlerModules);
		}

		public ValueTask<Position> Handle(object command, CancellationToken cancellationToken = default) =>
			_resolver.Invoke(command).Handler(command, cancellationToken);
	}
}

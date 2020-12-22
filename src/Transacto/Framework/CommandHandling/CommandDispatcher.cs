using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Transacto.Framework.CommandHandling {
	public class CommandDispatcher {
		private readonly MessageHandlerResolver<Checkpoint> _resolver;

		public CommandDispatcher(IEnumerable<CommandHandlerModule> commandHandlerModules) {
			_resolver = CommandResolve.WhenEqualToHandlerMessageType(commandHandlerModules);
		}

		public ValueTask<Checkpoint> Handle(object command, CancellationToken cancellationToken = default) =>
			_resolver.Invoke(command).Handler(command, cancellationToken);
	}
}

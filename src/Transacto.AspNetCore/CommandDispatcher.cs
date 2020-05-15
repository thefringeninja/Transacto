using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Transacto.Framework;

namespace Transaction.AspNetCore {
	internal class CommandDispatcher {
		private readonly CommandHandlerResolver _resolver;

		public CommandDispatcher(IEnumerable<CommandHandlerModule> commandHandlerModules) {
			_resolver = CommandResolve.WhenEqualToHandlerMessageType(commandHandlerModules);
		}

		public ValueTask Handle(object command, CancellationToken cancellationToken = default) =>
			_resolver.Invoke(command).Handler(command, cancellationToken);
	}
}

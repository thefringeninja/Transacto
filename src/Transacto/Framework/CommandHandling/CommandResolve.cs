using System.Collections.Generic;
using System.Linq;

namespace Transacto.Framework.CommandHandling {
	public static class CommandResolve {
		public static CommandHandlerResolver WhenEqualToHandlerMessageType(IEnumerable<CommandHandlerModule> modules) {
			var cache = modules.SelectMany(m => m.Handlers).ToLookup(h => h.Command);

			return command => {
				var type = command.GetType();

				var handlers = cache[type].ToArray();

				return handlers.Length != 1
					? throw new CommandResolveException(type, handlers.Length)
					: handlers.Single();
			};
		}
	}
}

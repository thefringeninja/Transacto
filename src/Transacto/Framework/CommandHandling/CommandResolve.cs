using System;
using System.Collections.Generic;
using System.Linq;

namespace Transacto.Framework.CommandHandling {
	public class CommandResolveException : Exception {

	}
	public static class CommandResolve {
		public static CommandHandlerResolver WhenEqualToHandlerMessageType(IEnumerable<CommandHandlerModule> modules) {
			var cache = modules.SelectMany(m => m.Handlers).ToLookup(h => h.Command);

			return command => {
				var handlers = cache[command.GetType()];

				return handlers.Count() != 1 ? throw new CommandResolveException() : handlers.Single();
			};
		}
	}
}

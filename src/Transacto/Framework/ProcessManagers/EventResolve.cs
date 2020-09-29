using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventStore.Client;
using Transacto.Framework.EventHandling;

namespace Transacto.Framework.ProcessManagers {
	public static class ProcessManagerEventResolve {
		public static MessageHandlerResolver<Position> WhenEqualToHandlerMessageType(
			ProcessManagerEventHandlerModule module) {
			var cache = module.ToLookup(h => h.Message);

			return @event => {
				var type = @event.GetType();

				var handlers = cache[type].ToArray();

				return handlers.Length switch {
					0 => new MessageHandler<Position>(type, (e, token) => new ValueTask<Position>(Position.Start)),
					1 => new MessageHandler<Position>(type, (e, ct) => handlers[0].Handler(e, ct)),
					_ => throw new InvalidOperationException()
				};
			};
		}
	}
}

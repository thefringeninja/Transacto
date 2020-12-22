using System;
using System.Linq;
using System.Threading.Tasks;

namespace Transacto.Framework.ProcessManagers {
	public static class ProcessManagerEventResolve {
		public static MessageHandlerResolver<Checkpoint> WhenEqualToHandlerMessageType(
			ProcessManagerEventHandlerModule module) {
			var cache = module.ToLookup(h => h.Message);

			return @event => {
				var type = @event.GetType();

				var handlers = cache[type].ToArray();

				return handlers.Length switch {
					0 => new MessageHandler<Checkpoint>(type, (_, _) => new ValueTask<Checkpoint>(Checkpoint.None)),
					1 => new MessageHandler<Checkpoint>(type, handlers[0].Handler),
					_ => throw new InvalidOperationException()
				};
			};
		}
	}
}

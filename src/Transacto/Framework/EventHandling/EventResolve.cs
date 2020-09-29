using System.Collections.Generic;
using System.Linq;

namespace Transacto.Framework.EventHandling {
	public static class EventResolve {
		public static MessageHandlerResolver<Unit> WhenEqualToHandlerMessageType(
			IEnumerable<EventHandlerModule> modules) {
			var cache = modules.SelectMany(m => m.Handlers).ToLookup(h => h.Message);

			return @event => {
				var type = @event.GetType();

				var handlers = cache[type].ToArray();

				return new MessageHandler<Unit>(type, async (e, ct) => {
					foreach (var handler in handlers) {
						await handler.Handler(e, ct);
					}

					return Unit.Instance;
				});
			};
		}
	}
}

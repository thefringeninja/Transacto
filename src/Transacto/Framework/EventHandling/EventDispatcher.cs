using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Transacto.Framework.EventHandling {
	public class EventDispatcher {
		private readonly MessageHandlerResolver<Unit> _resolver;

		public EventDispatcher(IEnumerable<EventHandlerModule> eventHandlerModules) {
			_resolver = EventResolve.WhenEqualToHandlerMessageType(eventHandlerModules);
		}

		public ValueTask<Unit> Handle(object @event, CancellationToken cancellationToken = default) =>
			_resolver.Invoke(@event).Handler(@event, cancellationToken);
	}
}

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EventStore.Client;

namespace Transacto.Framework.ProcessManagers {
	public class ProcessManagerEventDispatcher {
		private readonly MessageHandlerResolver<Position> _resolver;

		public ProcessManagerEventDispatcher(ProcessManagerEventHandlerModule eventHandlerModule) {
			_resolver = ProcessManagerEventResolve.WhenEqualToHandlerMessageType(eventHandlerModule);
		}

		public ValueTask<Position> Handle(object @event, CancellationToken cancellationToken = default) =>
			_resolver.Invoke(@event).Handler(@event, cancellationToken);
	}
}

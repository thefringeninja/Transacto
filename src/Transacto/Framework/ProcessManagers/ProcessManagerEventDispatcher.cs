using System.Threading;
using System.Threading.Tasks;

namespace Transacto.Framework.ProcessManagers {
	public class ProcessManagerEventDispatcher {
		private readonly MessageHandlerResolver<Checkpoint> _resolver;

		public ProcessManagerEventDispatcher(ProcessManagerEventHandlerModule eventHandlerModule) {
			_resolver = ProcessManagerEventResolve.WhenEqualToHandlerMessageType(eventHandlerModule);
		}

		public ValueTask<Checkpoint> Handle(object @event, CancellationToken cancellationToken = default) =>
			_resolver.Invoke(@event).Handler(@event, cancellationToken);
	}
}

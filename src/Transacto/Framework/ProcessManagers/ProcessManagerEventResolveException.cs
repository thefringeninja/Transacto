using System;

namespace Transacto.Framework.ProcessManagers {
	public class ProcessManagerEventResolveException : Exception {
		public Type EventType { get; }
		public int HandlerCount { get; }

		public ProcessManagerEventResolveException(Type eventType, int handlerCount)
			: base($"Expected one registration for event {eventType}, {handlerCount} found.") {
			EventType = eventType;
			HandlerCount = handlerCount;
		}
	}
}

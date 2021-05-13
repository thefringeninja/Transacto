using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Transacto.Framework.ProcessManagers {
	public class ProcessManagerEventHandlerModule : IEnumerable<MessageHandler<Checkpoint>> {
		private readonly List<MessageHandler<Checkpoint>> _handlers;

		protected ProcessManagerEventHandlerModule() {
			_handlers = new List<MessageHandler<Checkpoint>>();
		}

		protected IMessageHandlerBuilder<TEvent, Checkpoint> Build<TEvent>() where TEvent : class =>
			new MessageHandlerBuilder<TEvent, Checkpoint>(handler => {
				_handlers.Add(new MessageHandler<Checkpoint>(typeof(TEvent),
					(command, token) => handler((TEvent)command, token)));
			});

		protected void Handle<TEvent>(Func<TEvent, CancellationToken, ValueTask<Checkpoint>> handler) =>
			_handlers.Add(new MessageHandler<Checkpoint>(typeof(TEvent),
				(command, token) => handler((TEvent)command, token)));

		public MessageHandler<Checkpoint>[] Handlers => _handlers.ToArray();

		public MessageHandlerEnumerator<Checkpoint> GetEnumerator() => new(Handlers);

		IEnumerator<MessageHandler<Checkpoint>> IEnumerable<MessageHandler<Checkpoint>>.GetEnumerator() => GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}
}

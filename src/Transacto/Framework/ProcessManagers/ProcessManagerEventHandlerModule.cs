using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EventStore.Client;

namespace Transacto.Framework.ProcessManagers {
	public class ProcessManagerEventHandlerModule : IEnumerable<MessageHandler<Position>> {
		private readonly List<MessageHandler<Position>> _handlers;

		protected ProcessManagerEventHandlerModule() {
			_handlers = new List<MessageHandler<Position>>();
		}

		protected IMessageHandlerBuilder<TEvent, Position> Build<TEvent>() where TEvent : class =>
			new MessageHandlerBuilder<TEvent, Position>(handler => {
				_handlers.Add(new MessageHandler<Position>(typeof(TEvent),
					(command, token) => handler((TEvent)command, token)));
			});

		protected void Handle<TEvent>(Func<TEvent, CancellationToken, ValueTask<Position>> handler) =>
			_handlers.Add(new MessageHandler<Position>(typeof(TEvent),
				(command, token) => handler((TEvent)command, token)));

		public MessageHandler<Position>[] Handlers => _handlers.ToArray();

		public MessageHandlerEnumerator<Position> GetEnumerator() => new MessageHandlerEnumerator<Position>(Handlers);

		IEnumerator<MessageHandler<Position>> IEnumerable<MessageHandler<Position>>.GetEnumerator() => GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}
}

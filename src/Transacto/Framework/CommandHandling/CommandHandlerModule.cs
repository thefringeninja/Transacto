using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EventStore.Client;

namespace Transacto.Framework.CommandHandling {
    public abstract class CommandHandlerModule : IEnumerable<MessageHandler<Position>> {
        private readonly List<MessageHandler<Position>> _handlers;

        protected CommandHandlerModule() {
            _handlers = new List<MessageHandler<Position>>();
        }

        protected IMessageHandlerBuilder<TCommand, Position> Build<TCommand>() where TCommand : class =>
	        new MessageHandlerBuilder<TCommand, Position>(handler => {
		        _handlers.Add(new MessageHandler<Position>(typeof(TCommand),
			        (command, token) => handler((TCommand)command, token)));
	        });

        protected void Handle<TCommand>(Func<TCommand, CancellationToken, ValueTask<Position>> handler) =>
	        _handlers.Add(new MessageHandler<Position>(typeof(TCommand), (command, token) => handler((TCommand)command, token)));

        public MessageHandler<Position>[] Handlers => _handlers.ToArray();

        public MessageHandlerEnumerator<Position> GetEnumerator() => new(Handlers);

        IEnumerator<MessageHandler<Position>> IEnumerable<MessageHandler<Position>>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}

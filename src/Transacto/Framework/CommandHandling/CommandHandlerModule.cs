using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Transacto.Framework.CommandHandling {
    public abstract class CommandHandlerModule : IEnumerable<MessageHandler<Checkpoint>> {
        private readonly List<MessageHandler<Checkpoint>> _handlers;

        protected CommandHandlerModule() {
            _handlers = new List<MessageHandler<Checkpoint>>();
        }

        protected IMessageHandlerBuilder<TCommand, Checkpoint> Build<TCommand>() where TCommand : class =>
	        new MessageHandlerBuilder<TCommand, Checkpoint>(handler => {
		        _handlers.Add(new MessageHandler<Checkpoint>(typeof(TCommand),
			        (command, token) => handler((TCommand)command, token)));
	        });

        protected void Handle<TCommand>(Func<TCommand, CancellationToken, ValueTask<Checkpoint>> handler) =>
	        _handlers.Add(new MessageHandler<Checkpoint>(typeof(TCommand), (command, token) => handler((TCommand)command, token)));

        public MessageHandler<Checkpoint>[] Handlers => _handlers.ToArray();

        public MessageHandlerEnumerator<Checkpoint> GetEnumerator() => new MessageHandlerEnumerator<Checkpoint>(Handlers);

        IEnumerator<MessageHandler<Checkpoint>> IEnumerable<MessageHandler<Checkpoint>>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}

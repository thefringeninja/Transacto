using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EventStore.Client;

namespace Transacto.Framework.CommandHandling {
    public abstract class CommandHandlerModule : IEnumerable<CommandHandler> {
        private readonly List<CommandHandler> _handlers;

        protected CommandHandlerModule() {
            _handlers = new List<CommandHandler>();
        }

        protected ICommandHandlerBuilder<TCommand> Build<TCommand>() where TCommand : class =>
	        new CommandHandlerBuilder<TCommand>(handler => {
		        _handlers.Add(new CommandHandler(typeof(TCommand),
			        (command, token) => handler((TCommand)command, token)));
	        });

        protected void Handle<TCommand>(Func<TCommand, CancellationToken, ValueTask<Position>> handler) =>
	        _handlers.Add(new CommandHandler(typeof(TCommand), (command, token) => handler((TCommand)command, token)));

        public CommandHandler[] Handlers => _handlers.ToArray();

        public CommandHandlerEnumerator GetEnumerator() => new CommandHandlerEnumerator(Handlers);

        IEnumerator<CommandHandler> IEnumerable<CommandHandler>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}

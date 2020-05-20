using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Transacto.Framework.CommandHandling {
	public abstract class CommandHandlerModule<TMetadata> : IEnumerable<CommandHandler<TMetadata>> {
		private readonly List<CommandHandler<TMetadata>> _handlers;

		protected CommandHandlerModule() {
			_handlers = new List<CommandHandler<TMetadata>>();
		}

		protected ICommandHandlerBuilder<TCommand, TMetadata> Build<TCommand>() =>
			new CommandHandlerBuilder<TCommand, TMetadata>(handler => {
				_handlers.Add(new CommandHandler<TMetadata>(typeof(TCommand),
					(command, metadata, token) => handler((TCommand)command, metadata, token)));
			});

		protected void Handle<TCommand>(Func<TCommand, TMetadata, CancellationToken, ValueTask> handler) =>
			_handlers.Add(new CommandHandler<TMetadata>(typeof(TCommand),
				(command, metadata, token) => handler((TCommand)command, metadata, token)));

		public CommandHandler<TMetadata>[] Handlers => _handlers.ToArray();

		public CommandHandlerEnumerator<TMetadata> GetEnumerator() => new CommandHandlerEnumerator<TMetadata>(Handlers);

		IEnumerator<CommandHandler<TMetadata>> IEnumerable<CommandHandler<TMetadata>>.GetEnumerator() =>
			GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}
}

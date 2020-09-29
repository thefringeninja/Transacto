using System;
using System.Threading;
using System.Threading.Tasks;

namespace Transacto.Framework {
	public class MessageHandler<TReturn> {
		public MessageHandler(Type message, Func<object, CancellationToken, ValueTask<TReturn>> handler) {
			Message = message;
			Handler = handler;
		}

		public Type Message { get; }
		public Func<object, CancellationToken, ValueTask<TReturn>> Handler { get; }

		public MessageHandler<TReturn> Pipe(Func<
			Func<object, CancellationToken, ValueTask<TReturn>>,
			Func<object, CancellationToken, ValueTask<TReturn>>> pipe) => new MessageHandler<TReturn>(Message, pipe(Handler));
	}
}

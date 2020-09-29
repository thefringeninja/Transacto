using System;
using System.Threading;
using System.Threading.Tasks;

namespace Transacto.Framework {
	public interface IMessageHandlerBuilder<TMessage, TReturn> {
		IMessageHandlerBuilder<TMessage, TReturn> Pipe(
			Func<Func<TMessage, CancellationToken, ValueTask<TReturn>>,
				Func<TMessage, CancellationToken, ValueTask<TReturn>>> pipe);

		IMessageHandlerBuilder<TNext, TReturn> Transform<TNext>(
			Func<Func<TNext, CancellationToken, ValueTask<TReturn>>,
				Func<TMessage, CancellationToken, ValueTask<TReturn>>> pipe);

		void Handle(Func<TMessage, CancellationToken, ValueTask<TReturn>> handler);
	}
}

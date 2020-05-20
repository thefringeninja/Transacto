using System;
using System.Threading;
using System.Threading.Tasks;
using EventStore.Client;

namespace Transacto.Framework.CommandHandling {
	public interface ICommandHandlerBuilder<TCommand> {
		ICommandHandlerBuilder<TCommand> Pipe(
			Func<Func<TCommand, CancellationToken, ValueTask<Position>>,
				Func<TCommand, CancellationToken, ValueTask<Position>>> pipe);

		ICommandHandlerBuilder<TNext> Transform<TNext>(
			Func<Func<TNext, CancellationToken, ValueTask<Position>>,
				Func<TCommand, CancellationToken, ValueTask<Position>>> pipe);

		void Handle(Func<TCommand, CancellationToken, ValueTask<Position>> handler);
	}
}

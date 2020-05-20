using System;
using System.Threading;
using System.Threading.Tasks;
using EventStore.Client;

namespace Transacto.Framework.CommandHandling {
	internal class CommandHandlerBuilder<TCommand> : ICommandHandlerBuilder<TCommand> {
		private readonly Action<Func<TCommand, CancellationToken, ValueTask<Position>>> _build;

		public CommandHandlerBuilder(Action<Func<TCommand, CancellationToken, ValueTask<Position>>> build) {
			_build = build;
		}

		public ICommandHandlerBuilder<TCommand> Pipe(
			Func<Func<TCommand, CancellationToken, ValueTask<Position>>,
				Func<TCommand, CancellationToken, ValueTask<Position>>> pipe) =>
			new WithPipeline(_build, pipe);

		public ICommandHandlerBuilder<TNext> Transform<TNext>(
			Func<Func<TNext, CancellationToken, ValueTask<Position>>,
				Func<TCommand, CancellationToken, ValueTask<Position>>> pipe) =>
			new WithTransformedPipeline<TNext>(_build, pipe);

		public void Handle(Func<TCommand, CancellationToken, ValueTask<Position>> handler) => _build(handler);

		private class WithPipeline : ICommandHandlerBuilder<TCommand> {
			private readonly Action<Func<TCommand, CancellationToken, ValueTask<Position>>> _build;

			private readonly Func<Func<TCommand, CancellationToken, ValueTask<Position>>,
					Func<TCommand, CancellationToken, ValueTask<Position>>>
				_pipeline;

			public WithPipeline(Action<Func<TCommand, CancellationToken, ValueTask<Position>>> build,
				Func<Func<TCommand, CancellationToken, ValueTask<Position>>,
						Func<TCommand, CancellationToken, ValueTask<Position>>>
					pipeline) {
				_build = build;
				_pipeline = pipeline;
			}

			public ICommandHandlerBuilder<TCommand> Pipe(
				Func<Func<TCommand, CancellationToken, ValueTask<Position>>,
						Func<TCommand, CancellationToken, ValueTask<Position>>>
					pipe) =>
				new WithPipeline(_build, next => _pipeline(pipe(next)));

			public ICommandHandlerBuilder<TNext> Transform<TNext>(
				Func<Func<TNext, CancellationToken, ValueTask<Position>>,
					Func<TCommand, CancellationToken, ValueTask<Position>>> pipe) =>
				new WithTransformedPipeline<TNext>(_build, next => _pipeline(pipe(next)));

			public void Handle(Func<TCommand, CancellationToken, ValueTask<Position>> handler) =>
				_build(_pipeline(handler));
		}

		private class WithTransformedPipeline<TCurrent> : ICommandHandlerBuilder<TCurrent> {
			private readonly Action<Func<TCommand, CancellationToken, ValueTask<Position>>> _build;

			private readonly Func<Func<TCurrent, CancellationToken, ValueTask<Position>>,
					Func<TCommand, CancellationToken, ValueTask<Position>>>
				_pipeline;

			public WithTransformedPipeline(Action<Func<TCommand, CancellationToken, ValueTask<Position>>> build,
				Func<Func<TCurrent, CancellationToken, ValueTask<Position>>,
					Func<TCommand, CancellationToken, ValueTask<Position>>> pipeline) {
				_build = build;
				_pipeline = pipeline;
			}

			public ICommandHandlerBuilder<TCurrent> Pipe(
				Func<Func<TCurrent, CancellationToken, ValueTask<Position>>,
						Func<TCurrent, CancellationToken, ValueTask<Position>>>
					pipe) =>
				new WithTransformedPipeline<TCurrent>(_build, next => _pipeline(pipe(next)));

			public ICommandHandlerBuilder<TNext> Transform<TNext>(
				Func<Func<TNext, CancellationToken, ValueTask<Position>>,
						Func<TCurrent, CancellationToken, ValueTask<Position>>>
					pipe) =>
				new WithTransformedPipeline<TNext>(_build, next => _pipeline(pipe(next)));

			public void Handle(Func<TCurrent, CancellationToken, ValueTask<Position>> handler) =>
				_build(_pipeline(handler));
		}
	}
}

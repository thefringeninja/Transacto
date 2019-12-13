using System;
using System.Threading;
using System.Threading.Tasks;

namespace Transacto.Framework {
	internal class CommandHandlerBuilder<TCommand> : ICommandHandlerBuilder<TCommand> {
		private readonly Action<Func<TCommand, CancellationToken, ValueTask>> _build;

		public CommandHandlerBuilder(Action<Func<TCommand, CancellationToken, ValueTask>> build) {
			_build = build;
		}

		public ICommandHandlerBuilder<TCommand> Pipe(
			Func<Func<TCommand, CancellationToken, ValueTask>, Func<TCommand, CancellationToken, ValueTask>> pipe) =>
			new WithPipeline(_build, pipe);

		public ICommandHandlerBuilder<TNext> Transform<TNext>(
			Func<Func<TNext, CancellationToken, ValueTask>, Func<TCommand, CancellationToken, ValueTask>> pipe) =>
			new WithTransformedPipeline<TNext>(_build, pipe);

		public void Handle(Func<TCommand, CancellationToken, ValueTask> handler) => _build(handler);

		private class WithPipeline : ICommandHandlerBuilder<TCommand> {
			private readonly Action<Func<TCommand, CancellationToken, ValueTask>> _build;

			private readonly Func<Func<TCommand, CancellationToken, ValueTask>,
					Func<TCommand, CancellationToken, ValueTask>>
				_pipeline;

			public WithPipeline(Action<Func<TCommand, CancellationToken, ValueTask>> build,
				Func<Func<TCommand, CancellationToken, ValueTask>, Func<TCommand, CancellationToken, ValueTask>>
					pipeline) {
				_build = build;
				_pipeline = pipeline;
			}

			public ICommandHandlerBuilder<TCommand> Pipe(
				Func<Func<TCommand, CancellationToken, ValueTask>, Func<TCommand, CancellationToken, ValueTask>>
					pipe) =>
				new WithPipeline(_build, next => _pipeline(pipe(next)));

			public ICommandHandlerBuilder<TNext> Transform<TNext>(
				Func<Func<TNext, CancellationToken, ValueTask>, Func<TCommand, CancellationToken, ValueTask>> pipe) =>
				new WithTransformedPipeline<TNext>(_build, next => _pipeline(pipe(next)));

			public void Handle(Func<TCommand, CancellationToken, ValueTask> handler) => _build(_pipeline(handler));
		}

		private class WithTransformedPipeline<TCurrent> : ICommandHandlerBuilder<TCurrent> {
			private readonly Action<Func<TCommand, CancellationToken, ValueTask>> _build;

			private readonly Func<Func<TCurrent, CancellationToken, ValueTask>,
					Func<TCommand, CancellationToken, ValueTask>>
				_pipeline;

			public WithTransformedPipeline(Action<Func<TCommand, CancellationToken, ValueTask>> build,
				Func<Func<TCurrent, CancellationToken, ValueTask>, Func<TCommand, CancellationToken, ValueTask>>
					pipeline) {
				_build = build;
				_pipeline = pipeline;
			}

			public ICommandHandlerBuilder<TCurrent> Pipe(
				Func<Func<TCurrent, CancellationToken, ValueTask>, Func<TCurrent, CancellationToken, ValueTask>>
					pipe) =>
				new WithTransformedPipeline<TCurrent>(_build, next => _pipeline(pipe(next)));

			public ICommandHandlerBuilder<TNext> Transform<TNext>(
				Func<Func<TNext, CancellationToken, ValueTask>, Func<TCurrent, CancellationToken, ValueTask>> pipe) =>
				new WithTransformedPipeline<TNext>(_build, next => _pipeline(pipe(next)));

			public void Handle(Func<TCurrent, CancellationToken, ValueTask> handler) => _build(_pipeline(handler));
		}
	}
}

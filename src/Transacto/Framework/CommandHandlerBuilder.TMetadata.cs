using System;
using System.Threading;
using System.Threading.Tasks;

namespace Transacto.Framework {
	internal class CommandHandlerBuilder<TCommand, TMetadata> : ICommandHandlerBuilder<TCommand, TMetadata> {
		private readonly Action<Func<TCommand, TMetadata, CancellationToken, ValueTask>> _build;

		public CommandHandlerBuilder(Action<Func<TCommand, TMetadata, CancellationToken, ValueTask>> build) {
			_build = build;
		}

		public ICommandHandlerBuilder<TCommand, TMetadata> Pipe(
			Func<Func<TCommand, TMetadata, CancellationToken, ValueTask>,
				Func<TCommand, TMetadata, CancellationToken, ValueTask>> pipe) => new WithPipeline(_build, pipe);

		public ICommandHandlerBuilder<TNext, TMetadata> Transform<TNext>(
			Func<Func<TNext, TMetadata, CancellationToken, ValueTask>,
				Func<TCommand, TMetadata, CancellationToken, ValueTask>> pipe) =>
			new WithTransformedPipeline<TNext>(_build, pipe);

		public void Handle(Func<TCommand, TMetadata, CancellationToken, ValueTask> handler) => _build(handler);

		private class WithPipeline : ICommandHandlerBuilder<TCommand, TMetadata> {
			private readonly Action<Func<TCommand, TMetadata, CancellationToken, ValueTask>> _build;

			private readonly Func<Func<TCommand, TMetadata, CancellationToken, ValueTask>,
				Func<TCommand, TMetadata, CancellationToken, ValueTask>> _pipeline;

			public WithPipeline(Action<Func<TCommand, TMetadata, CancellationToken, ValueTask>> build,
				Func<Func<TCommand, TMetadata, CancellationToken, ValueTask>,
					Func<TCommand, TMetadata, CancellationToken, ValueTask>> pipeline) {
				_build = build;
				_pipeline = pipeline;
			}

			public ICommandHandlerBuilder<TCommand, TMetadata> Pipe(
				Func<Func<TCommand, TMetadata, CancellationToken, ValueTask>,
					Func<TCommand, TMetadata, CancellationToken, ValueTask>> pipe) =>
				new WithPipeline(_build, next => _pipeline(pipe(next)));

			public ICommandHandlerBuilder<TNext, TMetadata> Transform<TNext>(
				Func<Func<TNext, TMetadata, CancellationToken, ValueTask>,
					Func<TCommand, TMetadata, CancellationToken, ValueTask>
				> pipe) =>
				new WithTransformedPipeline<TNext>(_build, next => _pipeline(pipe(next)));

			public void Handle(Func<TCommand, TMetadata, CancellationToken, ValueTask> handler) =>
				_build(_pipeline(handler));
		}

		private class WithTransformedPipeline<TCurrent> : ICommandHandlerBuilder<TCurrent, TMetadata> {
			private readonly Action<Func<TCommand, TMetadata, CancellationToken, ValueTask>> _build;

			private readonly Func<Func<TCurrent, TMetadata, CancellationToken, ValueTask>,
				Func<TCommand, TMetadata, CancellationToken, ValueTask>> _pipeline;

			public WithTransformedPipeline(Action<Func<TCommand, TMetadata, CancellationToken, ValueTask>> build,
				Func<Func<TCurrent, TMetadata, CancellationToken, ValueTask>,
					Func<TCommand, TMetadata, CancellationToken, ValueTask>> pipeline) {
				_build = build;
				_pipeline = pipeline;
			}

			public ICommandHandlerBuilder<TCurrent, TMetadata> Pipe(
				Func<Func<TCurrent, TMetadata, CancellationToken, ValueTask>,
					Func<TCurrent, TMetadata, CancellationToken, ValueTask>> pipe) =>
				new WithTransformedPipeline<TCurrent>(_build, next => _pipeline(pipe(next)));

			public ICommandHandlerBuilder<TNext, TMetadata> Transform<TNext>(
				Func<Func<TNext, TMetadata, CancellationToken, ValueTask>,
					Func<TCurrent, TMetadata, CancellationToken, ValueTask>> pipe) =>
				new WithTransformedPipeline<TNext>(_build, next => _pipeline(pipe(next)));

			public void Handle(Func<TCurrent, TMetadata, CancellationToken, ValueTask> handler) =>
				_build(_pipeline(handler));
		}
	}
}

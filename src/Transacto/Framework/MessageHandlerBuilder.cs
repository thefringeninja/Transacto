namespace Transacto.Framework; 

internal class MessageHandlerBuilder<TMessage, TReturn> : IMessageHandlerBuilder<TMessage, TReturn> {
	private readonly Action<Func<TMessage, CancellationToken, ValueTask<TReturn>>> _build;

	public MessageHandlerBuilder(Action<Func<TMessage, CancellationToken, ValueTask<TReturn>>> build) {
		_build = build;
	}

	public IMessageHandlerBuilder<TMessage, TReturn> Pipe(
		Func<Func<TMessage, CancellationToken, ValueTask<TReturn>>,
			Func<TMessage, CancellationToken, ValueTask<TReturn>>> pipe) =>
		new WithPipeline(_build, pipe);

	public IMessageHandlerBuilder<TNext, TReturn> Transform<TNext>(
		Func<Func<TNext, CancellationToken, ValueTask<TReturn>>,
			Func<TMessage, CancellationToken, ValueTask<TReturn>>> pipe) =>
		new WithTransformedPipeline<TNext>(_build, pipe);

	public void Handle(Func<TMessage, CancellationToken, ValueTask<TReturn>> handler) => _build(handler);

	private class WithPipeline : IMessageHandlerBuilder<TMessage, TReturn> {
		private readonly Action<Func<TMessage, CancellationToken, ValueTask<TReturn>>> _build;

		private readonly Func<Func<TMessage, CancellationToken, ValueTask<TReturn>>,
				Func<TMessage, CancellationToken, ValueTask<TReturn>>>
			_pipeline;

		public WithPipeline(Action<Func<TMessage, CancellationToken, ValueTask<TReturn>>> build,
			Func<Func<TMessage, CancellationToken, ValueTask<TReturn>>,
					Func<TMessage, CancellationToken, ValueTask<TReturn>>>
				pipeline) {
			_build = build;
			_pipeline = pipeline;
		}

		public IMessageHandlerBuilder<TMessage, TReturn> Pipe(
			Func<Func<TMessage, CancellationToken, ValueTask<TReturn>>,
					Func<TMessage, CancellationToken, ValueTask<TReturn>>>
				pipe) =>
			new WithPipeline(_build, next => _pipeline(pipe(next)));

		public IMessageHandlerBuilder<TNext, TReturn> Transform<TNext>(
			Func<Func<TNext, CancellationToken, ValueTask<TReturn>>,
				Func<TMessage, CancellationToken, ValueTask<TReturn>>> pipe) =>
			new WithTransformedPipeline<TNext>(_build, next => _pipeline(pipe(next)));

		public void Handle(Func<TMessage, CancellationToken, ValueTask<TReturn>> handler) =>
			_build(_pipeline(handler));
	}

	private class WithTransformedPipeline<TCurrent> : IMessageHandlerBuilder<TCurrent, TReturn> {
		private readonly Action<Func<TMessage, CancellationToken, ValueTask<TReturn>>> _build;

		private readonly Func<Func<TCurrent, CancellationToken, ValueTask<TReturn>>,
				Func<TMessage, CancellationToken, ValueTask<TReturn>>>
			_pipeline;

		public WithTransformedPipeline(Action<Func<TMessage, CancellationToken, ValueTask<TReturn>>> build,
			Func<Func<TCurrent, CancellationToken, ValueTask<TReturn>>,
				Func<TMessage, CancellationToken, ValueTask<TReturn>>> pipeline) {
			_build = build;
			_pipeline = pipeline;
		}

		public IMessageHandlerBuilder<TCurrent, TReturn> Pipe(
			Func<Func<TCurrent, CancellationToken, ValueTask<TReturn>>,
					Func<TCurrent, CancellationToken, ValueTask<TReturn>>>
				pipe) =>
			new WithTransformedPipeline<TCurrent>(_build, next => _pipeline(pipe(next)));

		public IMessageHandlerBuilder<TNext, TReturn> Transform<TNext>(
			Func<Func<TNext, CancellationToken, ValueTask<TReturn>>,
					Func<TCurrent, CancellationToken, ValueTask<TReturn>>>
				pipe) =>
			new WithTransformedPipeline<TNext>(_build, next => _pipeline(pipe(next)));

		public void Handle(Func<TCurrent, CancellationToken, ValueTask<TReturn>> handler) =>
			_build(_pipeline(handler));
	}
}

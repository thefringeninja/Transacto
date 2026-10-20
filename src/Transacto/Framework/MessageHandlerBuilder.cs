namespace Transacto.Framework;

internal class MessageHandlerBuilder<TMessage, TReturn>(
	Action<Func<TMessage, CancellationToken, ValueTask<TReturn>>> build)
	: IMessageHandlerBuilder<TMessage, TReturn> {
	public IMessageHandlerBuilder<TMessage, TReturn> Pipe(
		Func<Func<TMessage, CancellationToken, ValueTask<TReturn>>,
			Func<TMessage, CancellationToken, ValueTask<TReturn>>> pipe) =>
		new WithPipeline(build, pipe);

	public IMessageHandlerBuilder<TNext, TReturn> Transform<TNext>(
		Func<Func<TNext, CancellationToken, ValueTask<TReturn>>,
			Func<TMessage, CancellationToken, ValueTask<TReturn>>> pipe) =>
		new WithTransformedPipeline<TNext>(build, pipe);

	public void Handle(Func<TMessage, CancellationToken, ValueTask<TReturn>> handler) => build(handler);

	private class WithPipeline(
		Action<Func<TMessage, CancellationToken, ValueTask<TReturn>>> build,
		Func<Func<TMessage, CancellationToken, ValueTask<TReturn>>,
				Func<TMessage, CancellationToken, ValueTask<TReturn>>>
			pipeline)
		: IMessageHandlerBuilder<TMessage, TReturn> {
		public IMessageHandlerBuilder<TMessage, TReturn> Pipe(
			Func<Func<TMessage, CancellationToken, ValueTask<TReturn>>,
					Func<TMessage, CancellationToken, ValueTask<TReturn>>>
				pipe) =>
			new WithPipeline(build, next => pipeline(pipe(next)));

		public IMessageHandlerBuilder<TNext, TReturn> Transform<TNext>(
			Func<Func<TNext, CancellationToken, ValueTask<TReturn>>,
				Func<TMessage, CancellationToken, ValueTask<TReturn>>> pipe) =>
			new WithTransformedPipeline<TNext>(build, next => pipeline(pipe(next)));

		public void Handle(Func<TMessage, CancellationToken, ValueTask<TReturn>> handler) =>
			build(pipeline(handler));
	}

	private class WithTransformedPipeline<TCurrent>(
		Action<Func<TMessage, CancellationToken, ValueTask<TReturn>>> build,
		Func<Func<TCurrent, CancellationToken, ValueTask<TReturn>>,
			Func<TMessage, CancellationToken, ValueTask<TReturn>>> pipeline)
		: IMessageHandlerBuilder<TCurrent, TReturn> {
		public IMessageHandlerBuilder<TCurrent, TReturn> Pipe(
			Func<Func<TCurrent, CancellationToken, ValueTask<TReturn>>,
					Func<TCurrent, CancellationToken, ValueTask<TReturn>>>
				pipe) =>
			new WithTransformedPipeline<TCurrent>(build, next => pipeline(pipe(next)));

		public IMessageHandlerBuilder<TNext, TReturn> Transform<TNext>(
			Func<Func<TNext, CancellationToken, ValueTask<TReturn>>,
					Func<TCurrent, CancellationToken, ValueTask<TReturn>>>
				pipe) =>
			new WithTransformedPipeline<TNext>(build, next => pipeline(pipe(next)));

		public void Handle(Func<TCurrent, CancellationToken, ValueTask<TReturn>> handler) =>
			build(pipeline(handler));
	}
}

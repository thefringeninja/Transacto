namespace Transacto.Framework;

public class MessageHandler<TReturn>(Type message, Func<object, CancellationToken, ValueTask<TReturn>> handler) {
	public Type Message { get; } = message;
	public Func<object, CancellationToken, ValueTask<TReturn>> Handler { get; } = handler;

	public MessageHandler<TReturn> Pipe(Func<
		Func<object, CancellationToken, ValueTask<TReturn>>,
		Func<object, CancellationToken, ValueTask<TReturn>>> pipe) => new(Message, pipe(Handler));
}

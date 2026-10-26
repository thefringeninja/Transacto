using System.Collections;

namespace Transacto.Framework.ProcessManagers;

public abstract class ProcessManagerEventHandlerModule : IEnumerable<MessageHandler<Checkpoint>> {
	private readonly List<MessageHandler<Checkpoint>> _handlers = [];
	private readonly HashSet<Type> _handledTypes = [];

	protected IMessageHandlerBuilder<TEvent, Checkpoint> Build<TEvent>() where TEvent : class =>
		new MessageHandlerBuilder<TEvent, Checkpoint>(handler => _handlers.Add(new MessageHandler<Checkpoint>(
			typeof(TEvent),
			(command, token) => handler((TEvent)command, token))));

	protected void Handle<TEvent>(Func<TEvent, CancellationToken, ValueTask<Checkpoint>> handler) {
		_handlers.Add(new MessageHandler<Checkpoint>(typeof(TEvent),
			(command, token) => handler((TEvent)command, token)));
		_handledTypes.Add(typeof(TEvent));
	}

	public MessageHandler<Checkpoint>[] Handlers => [.. _handlers];

	/// <summary>
	/// True when this module has a handler for <paramref name="messageType"/>.
	/// </summary>
	public bool Handles(Type messageType) => _handledTypes.Contains(messageType);

	public MessageHandlerEnumerator<Checkpoint> GetEnumerator() => new(Handlers);

	IEnumerator<MessageHandler<Checkpoint>> IEnumerable<MessageHandler<Checkpoint>>.GetEnumerator() => GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

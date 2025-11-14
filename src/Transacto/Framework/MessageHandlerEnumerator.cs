using System.Collections;

namespace Transacto.Framework;

public class MessageHandlerEnumerator<TReturn>(MessageHandler<TReturn>[] handlers)
	: IEnumerator<MessageHandler<TReturn>> {
	private int _index = -1;

	public bool MoveNext() => _index < handlers.Length &&
	                          ++_index < handlers.Length;

	public void Reset() => _index = -1;

	public MessageHandler<TReturn> Current => _index switch {
		-1 => throw new InvalidOperationException("Enumeration has not started. Call MoveNext."),
		var index when index == handlers.Length => throw new InvalidOperationException(
			"Enumeration has already ended. Call Reset."),
		_ => handlers[_index]
	};

	object IEnumerator.Current => Current;

	public void Dispose() {
	}
}

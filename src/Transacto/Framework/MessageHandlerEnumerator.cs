using System;
using System.Collections;
using System.Collections.Generic;

namespace Transacto.Framework {
	public class MessageHandlerEnumerator<TReturn> : IEnumerator<MessageHandler<TReturn>> {
		private readonly MessageHandler<TReturn>[] _handlers;
		private int _index;

		public MessageHandlerEnumerator(MessageHandler<TReturn>[] handlers) {
			_handlers = handlers;
			_index = -1;
		}

		public bool MoveNext() => _index < _handlers.Length &&
		                          ++_index < _handlers.Length;

		public void Reset() => _index = -1;

		public MessageHandler<TReturn> Current {
			get {
				if (_index == -1)
					throw new InvalidOperationException("Enumeration has not started. Call MoveNext.");
				if (_index == _handlers.Length)
					throw new InvalidOperationException("Enumeration has already ended. Call Reset.");

				return _handlers[_index];
			}
		}

		object IEnumerator.Current => Current;

		public void Dispose() {
		}
	}
}

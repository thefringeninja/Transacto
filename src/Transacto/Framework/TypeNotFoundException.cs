using System;

namespace Transacto.Framework {
	public class TypeNotFoundException : Exception {
		public Type Type { get; }

		public TypeNotFoundException(Type type, Exception? innerException = null)
			: base($"No storage type for '{type}' was found.", innerException) {
			Type = type;
		}
	}
}

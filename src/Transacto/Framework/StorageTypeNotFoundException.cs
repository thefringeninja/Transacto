using System;

namespace Transacto.Framework {
	public class StorageTypeNotFoundException : Exception {
		public string StorageType { get; }

		public StorageTypeNotFoundException(string storageType, Exception? innerException = null)
			: base($"No type for '{storageType}' was found.", innerException) {
			StorageType = storageType;
		}
	}
}

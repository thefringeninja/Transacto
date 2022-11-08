﻿using Transacto.Framework;

namespace Transacto.Testing;

internal static class Catch {
	public static Optional<Exception> Exception(Action action) {
		var result = Optional<Exception>.Empty;
		try {
			action();
		} catch (Exception exception) {
			result = new Optional<Exception>(exception);
		}

		return result;
	}
}

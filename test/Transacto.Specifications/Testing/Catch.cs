using Transacto.Framework;

namespace Transacto.Testing;

internal static class Catch {
	public static Option<Exception> Exception(Action action) {
		var result = Option<Exception>.None;
		try {
			action();
		} catch (Exception exception) {
			result = Option<Exception>.Some(exception);
		}

		return result;
	}
}

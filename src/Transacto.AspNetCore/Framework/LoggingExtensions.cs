using Microsoft.Extensions.Logging.Abstractions;

namespace Transacto.Framework;

public static class LoggingExtensions {
	public static IMessageHandlerBuilder<T, TReturn> Log<T, TReturn>(
		this IMessageHandlerBuilder<T, TReturn> builder,
		ILogger? logger = null) where T : class {
		logger ??= NullLogger.Instance;

		return builder.Pipe(next => async (m, ct) => {
			if (logger.IsEnabled(LogLevel.Information)) {
				logger.LogDebug("{message}", m.ToString());
			}

			try {
				return await next(m, ct);
			} catch (Exception ex) {
				if (logger.IsEnabled(LogLevel.Error)) {
					logger.LogError(ex, "Error handling message {message}", m.ToString());
				}

				throw;
			}
		});
	}
}

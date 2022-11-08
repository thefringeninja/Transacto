using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Transacto.Framework; 

public static class LoggingExtensions {
	public static IMessageHandlerBuilder<T, TReturn> Log<T, TReturn>(
		this IMessageHandlerBuilder<T, TReturn> builder,
		ILoggerFactory? loggerFactory = null) where T : class {
		var log = loggerFactory?.CreateLogger<T>() ?? new NullLogger<T>();

		return builder.Pipe(next => async (m, ct) => {
			if (log.IsEnabled(LogLevel.Information)) {
				log.LogInformation(m.ToString());
			}

			try {
				return await next(m, ct);
			} catch (Exception ex) {
				if (log.IsEnabled(LogLevel.Error)) {
					log.LogError(ex.ToString());
				}

				throw;
			}
		});
	}
}

using System;
using Transacto.Framework;
using Transacto.Logging;

namespace Transacto.Modules {
    internal static class LoggingExtensions {
        public static ICommandHandlerBuilder<T> Log<T>(this ICommandHandlerBuilder<T> builder) {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            var log = LogProvider.For<T>();

            return builder.Pipe(next => async (m, ct) => {
                log.Info(m.ToString);

                try {
                    await next(m, ct);
                } catch (Exception ex) {
                    log.Error(ex.ToString);
                }
            });
        }
    }
}

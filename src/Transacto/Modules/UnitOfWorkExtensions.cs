using System;
using System.Linq;
using System.Text.Json;
using EventStore.Grpc;
using Microsoft.Extensions.Logging;
using Transacto.Framework;

namespace Transacto.Modules {
    internal static class UnitOfWorkExtensions {
        public static ICommandHandlerBuilder<(UnitOfWork, TCommand)> UnitOfWork<TCommand>(
            this ICommandHandlerBuilder<TCommand> builder, EventStoreGrpcClient eventStore,
            JsonSerializerOptions options) {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (eventStore == null) throw new ArgumentNullException(nameof(eventStore));
            if (options == null) throw new ArgumentNullException(nameof(options));
            return builder.Transform<(UnitOfWork, TCommand)>(next => async (message, ct) => {
                var unitOfWork = new UnitOfWork();

                await next((unitOfWork, message), ct);

                if (!unitOfWork.HasChanges) {
                    return;
                }

                var (streamName, aggregateRoot, expectedVersion) = unitOfWork.GetChanges().Single();

                if (!expectedVersion.HasValue) {
                    await eventStore.AppendToStreamAsync(streamName,
                        AnyStreamRevision.NoStream,
                        aggregateRoot.GetChanges().Select(e => new EventData(Guid.NewGuid(), e.GetType().FullName,
                            JsonSerializer.SerializeToUtf8Bytes(e, options))), cancellationToken: ct);
                } else {
                    await eventStore.AppendToStreamAsync(streamName,
                        new StreamRevision(Convert.ToUInt64(expectedVersion.Value)),
                        aggregateRoot.GetChanges().Select(e => new EventData(Guid.NewGuid(), e.GetType().FullName,
                            JsonSerializer.SerializeToUtf8Bytes(e, options))), cancellationToken: ct);
                }
            });
        }
    }
}

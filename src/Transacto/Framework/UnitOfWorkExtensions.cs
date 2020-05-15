using System;
using System.Linq;
using System.Text.Json;
using EventStore.Client;

namespace Transacto.Framework {
	internal static class UnitOfWorkExtensions {
		public static ICommandHandlerBuilder<(UnitOfWork, TCommand)> UnitOfWork<TCommand>(
			this ICommandHandlerBuilder<TCommand> builder, EventStoreClient eventStore,
			IMessageTypeMapper messageTypeMapper, JsonSerializerOptions eventSerializerOptions)
			where TCommand : class =>
			builder.Transform<(UnitOfWork, TCommand)>(next => async (message, ct) => {
				var unitOfWork = new UnitOfWork();

				await next((unitOfWork, message), ct);

				if (!unitOfWork.HasChanges) {
					return;
				}

				var (streamName, aggregateRoot, expectedVersion) = unitOfWork.GetChanges().Single();

				if (!expectedVersion.HasValue) {
					await eventStore.AppendToStreamAsync(streamName,
						StreamState.NoStream,
						aggregateRoot.GetChanges().Select(e => new EventData(Uuid.NewUuid(),
							messageTypeMapper.Map(e.GetType()) ?? throw new InvalidOperationException(),
							JsonSerializer.SerializeToUtf8Bytes(e, eventSerializerOptions))),
						cancellationToken: ct);
				} else {
					await eventStore.AppendToStreamAsync(streamName,
						new StreamRevision(Convert.ToUInt64(expectedVersion.Value)),
						aggregateRoot.GetChanges().Select(e => new EventData(Uuid.NewUuid(),
							messageTypeMapper.Map(e.GetType()) ?? throw new InvalidOperationException(),
							JsonSerializer.SerializeToUtf8Bytes(e, eventSerializerOptions))),
						cancellationToken: ct);

				}

				aggregateRoot.MarkChangesAsCommitted();
			});
	}
}

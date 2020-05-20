using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using EventStore.Client;

namespace Transacto.Framework.CommandHandling {
	public static class UnitOfWorkExtensions {
		public static ICommandHandlerBuilder<(UnitOfWork, TCommand)> UnitOfWork<TCommand>(
			this ICommandHandlerBuilder<TCommand> builder, EventStoreClient eventStore,
			IMessageTypeMapper messageTypeMapper, JsonSerializerOptions eventSerializerOptions)
			where TCommand : class =>
			builder.Transform<(UnitOfWork, TCommand)>(next => async (message, ct) => {
				var unitOfWork = new UnitOfWork();

				await next((unitOfWork, message), ct);

				if (!unitOfWork.HasChanges) {
					return Position.Start;
				}

				var (streamName, aggregateRoot, expectedVersion) = unitOfWork.GetChanges().Single();

				var eventData = aggregateRoot.GetChanges().Select(e => new EventData(Uuid.NewUuid(),
					messageTypeMapper.Map(e.GetType()) ?? throw new InvalidOperationException(),
					JsonSerializer.SerializeToUtf8Bytes(e, eventSerializerOptions)));

				var result = await Append();

				aggregateRoot.MarkChangesAsCommitted();

				return result.LogPosition;

				Task<IWriteResult> Append() => expectedVersion.HasValue
					? eventStore.AppendToStreamAsync(streamName,
						new StreamRevision(Convert.ToUInt64(expectedVersion.Value)),
						eventData,
						options => options.TimeoutAfter = TimeSpan.FromMinutes(4),
						cancellationToken: ct)
					: eventStore.AppendToStreamAsync(streamName,
						StreamState.NoStream,
						eventData,
						options => options.TimeoutAfter = TimeSpan.FromMinutes(4),
						cancellationToken: ct);
			});
	}
}

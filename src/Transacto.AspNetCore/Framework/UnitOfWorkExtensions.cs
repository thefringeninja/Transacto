using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EventStore.Client;

namespace Transacto.Framework {
	public static class UnitOfWorkExtensions {
		public static IMessageHandlerBuilder<TCommand, Position> UnitOfWork<TCommand>(
			this IMessageHandlerBuilder<TCommand, Position> builder, EventStoreClient eventStore,
			IMessageTypeMapper messageTypeMapper, JsonSerializerOptions eventSerializerOptions)
			where TCommand : class => builder.Pipe(next => async (message, ct) => {
			using var _ = Framework.UnitOfWork.Start(eventStore, messageTypeMapper, eventSerializerOptions);

			await next(message, ct);

			if (!Framework.UnitOfWork.Current.HasChanges) {
				return Position.Start;
			}

			return await Commit(eventStore, messageTypeMapper, eventSerializerOptions, ct);
		});

		private static async Task<Position> Commit(EventStoreClient eventStore,
			IMessageTypeMapper messageTypeMapper, JsonSerializerOptions eventSerializerOptions, CancellationToken ct) {
			var (streamName, aggregateRoot, expectedVersion) = Framework.UnitOfWork.Current.GetChanges().Single();

			var eventData = aggregateRoot.GetChanges().Select(e => new EventData(Uuid.NewUuid(),
				messageTypeMapper.Map(e.GetType()),
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
		}
	}
}

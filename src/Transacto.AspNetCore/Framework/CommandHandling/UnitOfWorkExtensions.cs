using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using EventStore.Client;

namespace Transacto.Framework.CommandHandling {
	public static class UnitOfWorkExtensions {
		public static ICommandHandlerBuilder<TCommand> UnitOfWork<TCommand>(
			this ICommandHandlerBuilder<TCommand> builder, EventStoreClient eventStore,
			IMessageTypeMapper messageTypeMapper, JsonSerializerOptions eventSerializerOptions)
			where TCommand : class => builder.Pipe(next => async (message, ct) => {
			using var _ = CommandHandling.UnitOfWork.Start();

			await next(message, ct);

			if (!CommandHandling.UnitOfWork.Current.HasChanges) {
				return Position.Start;
			}

			var (streamName, aggregateRoot, expectedVersion) =
				CommandHandling.UnitOfWork.Current.GetChanges().Single();

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

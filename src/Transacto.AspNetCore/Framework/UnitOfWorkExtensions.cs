using System.Text.Json;
using EventStore.Client;
using Transacto.Infrastructure.EventStore;

namespace Transacto.Framework;

public static class UnitOfWorkExtensions {
	public static IMessageHandlerBuilder<TCommand, Checkpoint> UnitOfWork<TCommand>(
		this IMessageHandlerBuilder<TCommand, Checkpoint> builder, EventStoreClient eventStore,
		IMessageTypeMapper messageTypeMapper)
		where TCommand : class => builder.Pipe(next => async (message, ct) => {
		using var _ = Framework.UnitOfWork.Start();

		await next(message, ct);

		return !Framework.UnitOfWork.Current.HasChanges
			? Checkpoint.None
			: await Commit(eventStore, messageTypeMapper, ct);
	});

	private static async Task<Checkpoint> Commit(EventStoreClient eventStore,
		IMessageTypeMapper messageTypeMapper, CancellationToken ct) {
		var (streamName, aggregateRoot, expectedVersion) = Framework.UnitOfWork.Current.GetChanges().Single();

		var eventData = aggregateRoot.GetChanges().Select(ToEventData);

		var result =
			await eventStore.AppendToStreamAsync(streamName, expectedVersion, eventData, cancellationToken: ct);

		aggregateRoot.MarkChangesAsCommitted();

		return result.LogPosition.ToCheckpoint();

		EventData ToEventData(object e) {
			var type = e.GetType();

			return new EventData(Uuid.NewUuid(), messageTypeMapper.Map(type),
				JsonSerializer.SerializeToUtf8Bytes(e, type, TransactoSerializerOptions.Events));
		}
	}
}

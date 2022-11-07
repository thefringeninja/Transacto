using System.Text.Json;
using EventStore.Client;

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

		var eventData = aggregateRoot.GetChanges().Select(e => new EventData(Uuid.NewUuid(),
			messageTypeMapper.Map(e.GetType()),
			JsonSerializer.SerializeToUtf8Bytes(e, e.GetType(), TransactoSerializerOptions.Events)));

		var result = await Append();

		aggregateRoot.MarkChangesAsCommitted();

		return result.LogPosition.ToCheckpoint();

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

using System.Text.Json;
using KurrentDB.Client;
using Transacto.Framework;

namespace Transacto.Infrastructure.EventStore;

public class EventStoreRepository<TAggregateRoot>(
	KurrentDBClient eventStore,
	IMessageTypeMapper messageTypeMapper,
	JsonSerializerOptions? serializerOptions = null)
	where TAggregateRoot : AggregateRoot, IAggregateRoot<TAggregateRoot> {
	private readonly JsonSerializerOptions _serializerOptions = serializerOptions ?? TransactoSerializerOptions.Events;

	public async ValueTask<Optional<TAggregateRoot>> GetById(string identifier,
		CancellationToken cancellationToken = default) {
		if (UnitOfWork.Current.TryGet(identifier, out var a) && a is TAggregateRoot aggregate) {
			return new(aggregate);
		}

		var version = Optional<StreamState>.Empty;

		aggregate = TAggregateRoot.Factory();

		await foreach (var message in eventStore.ReadStreamAsync(Direction.Forwards, identifier, StreamPosition.Start,
			               cancellationToken: cancellationToken).Messages.WithCancellation(cancellationToken)) {
			switch (message) {
				case StreamMessage.NotFound:
					return Optional<TAggregateRoot>.Empty;
				case StreamMessage.Unknown:
					throw new InvalidOperationException();
				case StreamMessage.Event (var e):
					aggregate.ReadFromHistory(JsonSerializer.Deserialize(e.OriginalEvent.Data.Span,
						messageTypeMapper.Map(e.OriginalEvent.EventType),
						_serializerOptions) ?? throw new InvalidOperationException());
					version = version == Optional<StreamState>.Empty
						? StreamState.StreamRevision(0)
						: version.Value.Next();
					break;
			}
		}

		var expected = version switch {
			{ HasValue: true } => StreamState.StreamRevision((ulong)version.Value.ToInt64()),
			_ => StreamState.NoStream
		};

		UnitOfWork.Current.Attach(new(identifier, aggregate, expected));

		return new(aggregate);
	}

	public void Add(TAggregateRoot aggregateRoot) =>
		UnitOfWork.Current.Attach(new(aggregateRoot.Id, aggregateRoot, StreamState.NoStream));
}

using System.Text.Json;
using EventStore.Client;
using Transacto.Framework;

namespace Transacto.Infrastructure.EventStore;

public class EventStoreRepository<TAggregateRoot> where TAggregateRoot : AggregateRoot, IAggregateRoot<TAggregateRoot> {
	private static readonly JsonSerializerOptions DefaultOptions = new() {
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase
	};

	private readonly EventStoreClient _eventStore;
	private readonly IMessageTypeMapper _messageTypeMapper;
	private readonly JsonSerializerOptions _serializerOptions;

	public EventStoreRepository(
		EventStoreClient eventStore,
		IMessageTypeMapper messageTypeMapper,
		JsonSerializerOptions? serializerOptions = null) {
		_eventStore = eventStore;
		_messageTypeMapper = messageTypeMapper;
		_serializerOptions = serializerOptions ?? DefaultOptions;
	}

	public async ValueTask<Optional<TAggregateRoot>> GetById(string identifier,
		CancellationToken cancellationToken = default) {
		if (UnitOfWork.Current.TryGet(identifier, out var a) && a is TAggregateRoot aggregate) {
			return new(aggregate);
		}

		var version = Optional<StreamRevision>.Empty;

		aggregate = TAggregateRoot.Factory();

		await foreach (var message in _eventStore.ReadStreamAsync(Direction.Forwards, identifier, StreamPosition.Start,
			               cancellationToken: cancellationToken).Messages.WithCancellation(cancellationToken)) {
			switch (message) {
				case StreamMessage.NotFound:
					return Optional<TAggregateRoot>.Empty;
				case StreamMessage.Unknown:
					throw new InvalidOperationException();
				case StreamMessage.Event (var e):
					aggregate.ReadFromHistory(JsonSerializer.Deserialize(e.OriginalEvent.Data.Span,
						                          _messageTypeMapper.Map(e.OriginalEvent.EventType),
						                          _serializerOptions) ?? throw new InvalidOperationException());
					version = version == Optional<StreamRevision>.Empty ? 0 : version.Value.Next();
					break;
			}
		}

		var expected = version switch {
			{ HasValue: true } => new Expected.Revision(version.Value),
			_ => Expected.NoStream
		};

		UnitOfWork.Current.Attach(new(identifier, aggregate, expected));

		return new(aggregate);
	}

	public void Add(TAggregateRoot aggregateRoot) =>
		UnitOfWork.Current.Attach(new(aggregateRoot.Id, aggregateRoot, Expected.NoStream));
}

using System.Text.Json;
using KurrentDB.Client;
using Transacto.Framework;

namespace Transacto.Infrastructure.EventStore;

public class EventStoreRepository<TAggregateRoot>(
	KurrentDBClient eventStore,
	IMessageTypeMapper messageTypeMapper,
	JsonSerializerOptions? serializerOptions = null)
	where TAggregateRoot : AggregateRoot, IAggregateRoot<TAggregateRoot> {
	private readonly JsonSerializerOptions
		_serializerOptions = serializerOptions ?? TransactoSerializerOptions.Messages;

	public async ValueTask<Option<TAggregateRoot>> GetById(string identifier,
		CancellationToken cancellationToken = default) {
		if (UnitOfWork.Current.TryGet(identifier, out var a) && a is TAggregateRoot aggregate) {
			return new(aggregate);
		}

		var version = Option<StreamState>.None;

		aggregate = TAggregateRoot.Factory();

		await foreach (var message in eventStore.ReadStreamAsync(Direction.Forwards, identifier, StreamPosition.Start,
			               cancellationToken: cancellationToken).Messages.WithCancellation(cancellationToken)) {
			switch (message) {
				case StreamMessage.NotFound:
					return Option<TAggregateRoot>.None;
				case StreamMessage.Unknown:
					throw new InvalidOperationException();
				case StreamMessage.Event (var e):
					aggregate.ReadFromHistory(JsonSerializer.Deserialize(e.OriginalEvent.Data.Span,
						messageTypeMapper.Map(e.OriginalEvent.EventType),
						_serializerOptions) ?? throw new InvalidOperationException());
					version = version switch {
						Some<StreamState> some => Option<StreamState>.Some(some.Value.Next()),
						_ => Option<StreamState>.Some(StreamState.StreamRevision(0))
					};
					break;
			}
		}

		var expected = version switch {
			Some<StreamState> some => StreamState.StreamRevision((ulong)some.Value.ToInt64()),
			_ => StreamState.NoStream
		};

		UnitOfWork.Current.Attach(new(identifier, aggregate, expected));

		return new(aggregate);
	}

	public void Add(TAggregateRoot aggregateRoot) =>
		UnitOfWork.Current.Attach(new(aggregateRoot.Id, aggregateRoot, StreamState.NoStream));
}

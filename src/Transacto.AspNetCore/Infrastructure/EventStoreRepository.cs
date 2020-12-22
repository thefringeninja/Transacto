using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EventStore.Client;
using Transacto.Framework;

namespace Transacto.Infrastructure {
	public class EventStoreRepository<TAggregateRoot> where TAggregateRoot : AggregateRoot {
		private static readonly JsonSerializerOptions DefaultOptions = new() {
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase
		};

		private readonly EventStoreClient _eventStore;
		private readonly Func<TAggregateRoot> _factory;
		private readonly IMessageTypeMapper _messageTypeMapper;
		private readonly JsonSerializerOptions _serializerOptions;

		public EventStoreRepository(
			EventStoreClient eventStore,
			Func<TAggregateRoot> factory,
			IMessageTypeMapper messageTypeMapper,
			JsonSerializerOptions? serializerOptions = null) {
			_eventStore = eventStore;
			_factory = factory;
			_messageTypeMapper = messageTypeMapper;
			_serializerOptions = serializerOptions ?? DefaultOptions;
		}

		public async ValueTask<Optional<TAggregateRoot>> GetById(string identifier,
			CancellationToken cancellationToken = default) {
			var streamName = identifier;

			if (UnitOfWork.Current.TryGet(streamName, out var a) && a is TAggregateRoot aggregate) {
				return new Optional<TAggregateRoot>(aggregate);
			}

			await using var result = _eventStore.ReadStreamAsync(Direction.Forwards, streamName, StreamPosition.Start,
				configureOperationOptions: options => options.TimeoutAfter = TimeSpan.FromMinutes(20),
				cancellationToken: cancellationToken);

			if (await result.ReadState == ReadState.StreamNotFound) {
				return Optional<TAggregateRoot>.Empty;
			}

			aggregate = _factory();

			var version = await aggregate.LoadFromHistory(result.Select(e =>
				JsonSerializer.Deserialize(e.OriginalEvent.Data.Span,
					_messageTypeMapper.Map(e.OriginalEvent.EventType),
					_serializerOptions)!));

			UnitOfWork.Current.Attach(new (streamName, aggregate, version));

			return aggregate;
		}

		public void Add(TAggregateRoot aggregateRoot) =>
			UnitOfWork.Current.Attach(new (aggregateRoot.Id, aggregateRoot));
	}
}

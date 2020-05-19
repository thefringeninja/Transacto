using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EventStore.Client;
using Transacto.Framework;

namespace Transacto.Infrastructure {
    public class EventStoreRepository<TAggregateRoot> where TAggregateRoot : AggregateRoot {
        private static readonly JsonSerializerOptions DefaultOptions = new JsonSerializerOptions {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private readonly EventStoreClient _eventStore;
        private readonly UnitOfWork _unitOfWork;
        private readonly Func<TAggregateRoot> _factory;
        private readonly Func<string, string> _getStreamName;
        private readonly IMessageTypeMapper _messageTypeMapper;
        private readonly JsonSerializerOptions _serializerOptions;

        public EventStoreRepository(
            EventStoreClient eventStore,
            UnitOfWork unitOfWork,
            Func<TAggregateRoot> factory,
            Func<string, string> getStreamName,
            IMessageTypeMapper messageTypeMapper,
            JsonSerializerOptions? serializerOptions = null) {
            _eventStore = eventStore;
            _unitOfWork = unitOfWork;
            _factory = factory;
            _getStreamName = getStreamName;
            _messageTypeMapper = messageTypeMapper;
            _serializerOptions = serializerOptions ?? DefaultOptions;
        }

        public async ValueTask<Optional<TAggregateRoot>> GetById(string identifier,
            CancellationToken cancellationToken = default) {
            var streamName = _getStreamName(identifier);
            if (_unitOfWork.TryGet(streamName, out var a) && a is TAggregateRoot aggregate) {
                return new Optional<TAggregateRoot>(aggregate);
            }

            try {
                await using var events = _eventStore.ReadStreamAsync(Direction.Forwards,
                    streamName, StreamPosition.Start, int.MaxValue, cancellationToken: cancellationToken);

                aggregate = _factory();

                var version = await aggregate.LoadFromHistory(events.Select(e =>
                    JsonSerializer.Deserialize(e.OriginalEvent.Data.Span,
                        _messageTypeMapper.Map(e.OriginalEvent.EventType),
                        _serializerOptions)));

                _unitOfWork.Attach(streamName, aggregate, version);

                return aggregate;
            } catch (StreamNotFoundException) {
                return Optional<TAggregateRoot>.Empty;
            }
        }

        public void Add(TAggregateRoot aggregateRoot) =>
            _unitOfWork.Attach(_getStreamName(aggregateRoot.Id), aggregateRoot);
    }
}

using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EventStore.Grpc;
using Transacto.Domain;
using Transacto.Framework;

namespace Transacto.Infrastructure {
    public class EventStoreRepository<TAggregateRoot, TIdentifier> where TAggregateRoot : AggregateRoot {
        private static readonly JsonSerializerOptions DefaultOptions = new JsonSerializerOptions {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private readonly EventStoreGrpcClient _eventStore;
        private readonly UnitOfWork _unitOfWork;
        private readonly Func<TAggregateRoot> _factory;
        private readonly Func<TAggregateRoot, TIdentifier> _getIdentifier;
        private readonly Func<TIdentifier, string> _getStreamName;
        private readonly JsonSerializerOptions _serializerOptions;

        public EventStoreRepository(
            EventStoreGrpcClient eventStore,
            UnitOfWork unitOfWork,
            Func<TAggregateRoot> factory,
            Func<TAggregateRoot, TIdentifier> getIdentifier,
            Func<TIdentifier, string> getStreamName,
            JsonSerializerOptions serializerOptions = default) {
            if (eventStore == null) throw new ArgumentNullException(nameof(eventStore));
            if (unitOfWork == null) throw new ArgumentNullException(nameof(unitOfWork));
            if (getIdentifier == null) throw new ArgumentNullException(nameof(getIdentifier));
            if (getStreamName == null) throw new ArgumentNullException(nameof(getStreamName));
            _eventStore = eventStore;
            _unitOfWork = unitOfWork;
            _factory = factory;
            _getIdentifier = getIdentifier;
            _getStreamName = getStreamName;
            _serializerOptions = serializerOptions ?? DefaultOptions;
        }

        public async ValueTask<Optional<TAggregateRoot>> GetById(TIdentifier identifier,
            CancellationToken cancellationToken = default) {
            var streamName = _getStreamName(identifier);
            if (_unitOfWork.TryGet(streamName, out var a) && a is TAggregateRoot aggregate) {
                return new Optional<TAggregateRoot>(aggregate);
            }

            try {
                var events = _eventStore.ReadStreamForwardsAsync(
                    streamName, StreamRevision.Start, int.MaxValue, cancellationToken: cancellationToken);

                aggregate = _factory();

                var version = await aggregate.LoadFromHistory(events.Select(e =>
                    JsonSerializer.Deserialize(e.OriginalEvent.Data,
                        typeof(AccountingPeriod).Assembly.GetType(e.OriginalEvent.EventType),
                        _serializerOptions)));

                _unitOfWork.Attach(streamName, aggregate, version);

                return aggregate;
            } catch (StreamNotFoundException) {
                return Optional<TAggregateRoot>.Empty;
            }
        }

        public void Add(TAggregateRoot aggregateRoot) =>
            _unitOfWork.Attach(_getStreamName(_getIdentifier(aggregateRoot)), aggregateRoot);
    }
}

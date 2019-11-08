using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SqlStreamStore;
using SqlStreamStore.Streams;
using Transacto.Domain;
using Transacto.Framework;

namespace Transacto.Infrastructure {
    public class SqlStreamStoreBusinessTransactionRepository<TBusinessTransaction>
        where TBusinessTransaction : IBusinessTransaction {
        private readonly IStreamStore _streamStore;
        private readonly Func<TBusinessTransaction, string> _getStreamName;
        private readonly JsonSerializerOptions _serializerOptions;

        public SqlStreamStoreBusinessTransactionRepository(
            IStreamStore streamStore,
            Func<TBusinessTransaction, string> getStreamName,
            JsonSerializerOptions serializerOptions) {
            if (streamStore == null) throw new ArgumentNullException(nameof(streamStore));
            if (getStreamName == null) throw new ArgumentNullException(nameof(getStreamName));
            if (serializerOptions == null) throw new ArgumentNullException(nameof(serializerOptions));
            _streamStore = streamStore;
            _getStreamName = getStreamName;
            _serializerOptions = serializerOptions;
        }

        public async ValueTask<Optional<TBusinessTransaction>> GetOptional(string id,
            CancellationToken cancellationToken = default) {
            var page = await _streamStore.ReadStreamBackwards(id, StreamVersion.End, 1,
                cancellationToken: cancellationToken);

            if (page.Messages.Length == 0) {
                return Optional<TBusinessTransaction>.Empty;
            }

            var data = await page.Messages[0].GetJsonData(cancellationToken);

            return JsonSerializer.Deserialize<TBusinessTransaction>(data);
        }

        public async ValueTask<TBusinessTransaction> Get(string id, CancellationToken cancellationToken = default) {
            var optionalTransaction = await GetOptional(id, cancellationToken);
            if (!optionalTransaction.HasValue) {
                throw new InvalidOperationException();
            }

            return optionalTransaction.Value;
        }

        public ValueTask Save(TBusinessTransaction transaction, CancellationToken cancellationToken = default) {
            if (transaction == null) throw new ArgumentNullException(nameof(transaction));

            var streamName = _getStreamName(transaction);
            var data = JsonSerializer.Serialize(transaction, _serializerOptions);

            return new ValueTask(_streamStore.AppendToStream(streamName, ExpectedVersion.Any,
                new NewStreamMessage(Guid.NewGuid(), typeof(TBusinessTransaction).Name, data), cancellationToken));
        }
    }
}

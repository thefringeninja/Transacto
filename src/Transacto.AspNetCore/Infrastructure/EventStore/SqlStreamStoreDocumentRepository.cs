using System.Text.Json;
using SqlStreamStore;
using SqlStreamStore.Streams;
using Transacto.Domain;
using Transacto.Framework;

namespace Transacto.Infrastructure.EventStore; 

public class StreamStoreBusinessTransactionRepository<TBusinessTransaction>
	where TBusinessTransaction : IBusinessTransaction {
	private readonly IStreamStore _streamStore;
	private readonly Func<TBusinessTransaction, string> _getStreamName;
	private readonly JsonSerializerOptions _serializerOptions;

	public StreamStoreBusinessTransactionRepository(
		IStreamStore streamStore,
		Func<TBusinessTransaction, string> getStreamName,
		JsonSerializerOptions serializerOptions) {
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

		var document = page.Messages[0];

		var data = await document.GetJsonData(cancellationToken);

		var businessTransaction = JsonSerializer.Deserialize<TBusinessTransaction>(data);
		return businessTransaction switch {
			null => Optional<TBusinessTransaction>.Empty,
			_ => businessTransaction
		};
	}

	public async ValueTask<TBusinessTransaction> Get(string id, CancellationToken cancellationToken = default) {
		var optionalTransaction = await GetOptional(id, cancellationToken);
		return optionalTransaction.HasValue switch {
			false => throw new InvalidOperationException(),
			_ => optionalTransaction.Value
		};
	}

	public ValueTask Save(TBusinessTransaction transaction, int expectedVersion,
		CancellationToken cancellationToken = default) {
		var streamName = _getStreamName(transaction);
		var data = JsonSerializer.Serialize(transaction, _serializerOptions);

		return new ValueTask(_streamStore.AppendToStream(streamName, expectedVersion,
			new NewStreamMessage(Guid.NewGuid(), typeof(TBusinessTransaction).Name, data), cancellationToken));
	}
}

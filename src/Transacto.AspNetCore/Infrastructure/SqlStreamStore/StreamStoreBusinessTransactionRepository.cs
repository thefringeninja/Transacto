using System.Text.Json;
using SqlStreamStore;
using SqlStreamStore.Streams;
using Transacto.Domain;
using Transacto.Framework;

namespace Transacto.Infrastructure.SqlStreamStore;

public readonly record struct BusinessTransactionEnvelope<TBusinessTransaction>(
	Optional<TBusinessTransaction> BusinessTransaction, int ExpectedRevision = ExpectedVersion.NoStream - 1) {
	public static readonly BusinessTransactionEnvelope<TBusinessTransaction> Empty =
		new(Optional<TBusinessTransaction>.Empty);
}

public class StreamStoreBusinessTransactionRepository<TBusinessTransaction>
	where TBusinessTransaction : class, IBusinessTransaction {
	private readonly IStreamStore _streamStore;
	private readonly Func<TBusinessTransaction, string> _getStreamName;
	private readonly JsonSerializerOptions _serializerOptions;

	public StreamStoreBusinessTransactionRepository(IStreamStore streamStore,
		Func<TBusinessTransaction, string> getStreamName, JsonSerializerOptions serializerOptions) {
		_streamStore = streamStore;
		_getStreamName = getStreamName;
		_serializerOptions = serializerOptions;
	}

	public async ValueTask<BusinessTransactionEnvelope<TBusinessTransaction>> GetOptional(string id,
		CancellationToken cancellationToken = default) =>
		await _streamStore.ReadStreamBackwards(id, StreamVersion.End, 1,
			prefetchJsonData: true, cancellationToken: cancellationToken) switch {
			{ Messages.Length: 0 } => BusinessTransactionEnvelope<TBusinessTransaction>.Empty,
			var page => JsonSerializer.Deserialize<TBusinessTransaction>(await page.Messages[0]
					.GetJsonData(cancellationToken))
				switch {
					null => BusinessTransactionEnvelope<TBusinessTransaction>.Empty,
					var businessTransaction => new(businessTransaction, page.Messages[0].StreamVersion)
				}
		};

	public async ValueTask<BusinessTransactionEnvelope<TBusinessTransaction>> Get(string id,
		CancellationToken cancellationToken = default) =>
		await GetOptional(id, cancellationToken) switch {
			{ BusinessTransaction.HasValue: true } optionalTransaction => optionalTransaction,
			_ => throw new InvalidOperationException()
		};

	public ValueTask Save(TBusinessTransaction transaction, int expectedVersion,
		CancellationToken cancellationToken = default) {
		var streamName = _getStreamName(transaction);
		var data = JsonSerializer.Serialize(transaction, _serializerOptions);

		return new ValueTask(_streamStore.AppendToStream(streamName, expectedVersion,
			new NewStreamMessage(Guid.NewGuid(), typeof(TBusinessTransaction).Name, data), cancellationToken));
	}
}

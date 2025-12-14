using System.Text.Json;
using SqlStreamStore;
using SqlStreamStore.Streams;
using Transacto.Framework;

namespace Transacto.Infrastructure.SqlStreamStore;

public abstract class StreamStoreFeedProjection<TFeedEntry>(
	string streamName,
	IMessageTypeMapper messageTypeMapper,
	JsonSerializerOptions? serializerOptions = null)
	: StreamStoreProjection(streamName)
	where TFeedEntry : IFeedEntry {
	private readonly JsonSerializerOptions _serializerOptions = serializerOptions ?? TransactoSerializerOptions.Events;

	protected void When<TEvent>(Func<TEvent, TFeedEntry> apply)
		=> When<Envelope>(async (streamStore, e, ct) => {
			if (!messageTypeMapper.TryMap(typeof(TEvent), out var type)) {
				return;
			}

			if (e.Message is not TEvent o) {
				return;
			}

			var target = apply(o);

			await streamStore.AppendToStream(StreamName, ExpectedVersion.Any,
				new NewStreamMessage(Guid.NewGuid(), messageTypeMapper.Map(typeof(TEvent)),
					JsonSerializer.Serialize(target, _serializerOptions),
					JsonSerializer.Serialize(new Dictionary<string, string> {
						["commit"] = e.Position.CommitPosition.ToString(),
						["prepare"] = e.Position.PreparePosition.ToString()
					})), ct);
		});
}

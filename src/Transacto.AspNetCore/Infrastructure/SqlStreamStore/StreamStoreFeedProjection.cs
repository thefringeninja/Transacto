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

	protected void When<TEvent>(Func<TEvent, TFeedEntry, TFeedEntry> apply)
		=> When<Envelope>(async (streamStore, e, ct) => {
			var page = await streamStore.ReadStreamBackwards(StreamName, StreamVersion.End, 1,
				true, ct);

			var (target, expectedVersion) = page.Status == PageReadStatus.StreamNotFound
				? (Activator.CreateInstance<TFeedEntry>(), ExpectedVersion.NoStream)
				: (JsonSerializer.Deserialize<TFeedEntry>(await page.Messages[0].GetJsonData(ct))!,
					page.Messages[0].StreamVersion);

			if (!messageTypeMapper.TryMap(typeof(TEvent), out var type)) {
				type = "unknown";
			}

			target.Events.Add(type);

			await streamStore.AppendToStream(StreamName, expectedVersion,
				new NewStreamMessage(Guid.NewGuid(), messageTypeMapper.Map(typeof(TEvent)),
					JsonSerializer.Serialize(target, _serializerOptions),
					JsonSerializer.Serialize(new Dictionary<string, string> {
						["commit"] = e.Position.CommitPosition.ToString(),
						["prepare"] = e.Position.PreparePosition.ToString()
					})), ct);
		});
}

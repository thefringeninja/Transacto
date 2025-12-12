using System.Text.Json;
using KurrentDB.Client;
using Projac;
using SqlStreamStore;
using SqlStreamStore.Streams;
using Position = KurrentDB.Client.Position;

namespace Transacto.Infrastructure.SqlStreamStore;

public abstract class StreamStoreProjection : Projection<IStreamStore> {
	public string StreamName { get; }

	protected StreamStoreProjection(string streamName) {
		ArgumentException.ThrowIfNullOrWhiteSpace(streamName);
		StreamName = streamName;
	}

	public async ValueTask<FromAll> ReadCheckpoint(IStreamStore streamStore,
		CancellationToken cancellationToken) {
		var page = await streamStore.ReadStreamBackwards(StreamName, StreamVersion.End, 1,
			false, cancellationToken);

		if (page.Status == PageReadStatus.StreamNotFound) {
			return FromAll.Start;
		}

		var metadata = JsonSerializer.Deserialize<Dictionary<string, string>>(page.Messages[0].JsonMetadata)!;

		return metadata.TryGetValue("commit", out var commitValue) &&
		       ulong.TryParse(commitValue, out var commit) &&
		       metadata.TryGetValue("prepare", out var prepareValue) &&
		       ulong.TryParse(prepareValue, out var prepare)
			? FromAll.After(new Position(commit, prepare))
			: FromAll.Start;
	}
}

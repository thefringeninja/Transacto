using System.Text.Json;
using Projac;
using SqlStreamStore;
using SqlStreamStore.Streams;
using Position = EventStore.Client.Position;

namespace Transacto.Infrastructure.SqlStreamStore; 

public abstract class StreamStoreProjection : Projection<IStreamStore> {
	protected string StreamName { get; }

	protected StreamStoreProjection(string streamName) {
		if (string.IsNullOrWhiteSpace(streamName)) {
			throw new ArgumentException();
		}

		StreamName = streamName;
	}

	public async ValueTask<Position> ReadCheckpoint(IStreamStore streamStore,
		CancellationToken cancellationToken) {
		var page = await streamStore.ReadStreamBackwards(StreamName, StreamVersion.End, 1,
			false, cancellationToken);

		if (page.Status == PageReadStatus.StreamNotFound) {
			return Position.Start;
		}

		var metadata = JsonSerializer.Deserialize<Dictionary<string, string>>(page.Messages[0].JsonMetadata)!;

		return metadata.TryGetValue("commit", out var commitValue) &&
		       ulong.TryParse(commitValue, out var commit) &&
		       metadata.TryGetValue("prepare", out var prepareValue) &&
		       ulong.TryParse(prepareValue, out var prepare)
			? new Position(commit, prepare)
			: Position.Start;
	}
}

using System.Text;
using System.Threading.Channels;
using KurrentDB.Client;

var channel = Channel.CreateBounded<object>(new BoundedChannelOptions(3) {
	SingleReader = true,
	SingleWriter = false
});

const string contentType = "application/octet-stream";

Dictionary<string, StreamState> expectedVersions = [];

var settings = KurrentDBClientSettings.Create("esdb://localhost:2113/?tls=false");

await using var eventStore = new KurrentDBClient(settings);

var checkpoints = Checkpoints.Empty;

var fromAll = FromAll.Start;

_ = Subscribe();

await foreach (var resolvedEvent in eventStore.ReadAllAsync(Direction.Backwards, Position.End,
	               EventTypeFilter.Prefix("$et-", "$ce-"))) {
	if (checkpoints.ByCategory == Position.End && resolvedEvent.Event.EventStreamId[..3] == "$ce-") {
		checkpoints = checkpoints with {
			ByCategory = ReadFromMetadata(resolvedEvent.Event.Metadata)
		};
	} else if (checkpoints.ByType == Position.End && resolvedEvent.Event.EventStreamId[..3] == "$et-") {
		checkpoints = checkpoints with {
			ByType = ReadFromMetadata(resolvedEvent.Event.Metadata)
		};
	}

	if (checkpoints.ByCategory != Position.End && checkpoints.ByType != Position.End) {
		fromAll = FromAll.After(checkpoints.ByCategory < checkpoints.ByType
			? checkpoints.ByCategory
			: checkpoints.ByType);

		break;
	}
}

await using var subscription = eventStore.SubscribeToAll(fromAll,
	filterOptions: new SubscriptionFilterOptions(EventTypeFilter.ExcludeSystemEvents()));
await foreach (var message in subscription.Messages) {
	if (message is not StreamMessage.Event (var resolvedEvent)) {
		continue;
	}

	await channel.Writer.WriteAsync(resolvedEvent);
}

return;

Position ReadFromMetadata(ReadOnlyMemory<byte> metadata) =>
	new(BitConverter.ToUInt64(metadata.Span), BitConverter.ToUInt64(metadata.Span[8..]));

string GetCategory(string streamName) {
	return streamName.Split('-')[0];
}

async Task Subscribe() {
	var byEventTypeChannel = Channel.CreateUnbounded<object>(new() {
		SingleReader = true,
		SingleWriter = false
	});

	_ = Subscribe(byEventTypeChannel);

	var byCategory = Channel.CreateUnbounded<object>(new() {
		SingleReader = true,
		SingleWriter = false
	});

	_ = Subscribe(byCategory);

	await foreach (var message in channel.Reader.ReadAllAsync()) {
		switch (message) {
			case ResolvedEvent resolvedEvent:
				EventData eventData = GetLinkTo(resolvedEvent);

				byEventTypeChannel.Writer.TryWrite(($"$et-{resolvedEvent.Event.EventType}", eventData));
				byEventTypeChannel.Writer.TryWrite(($"$ce-{GetCategory(resolvedEvent.Event.EventStreamId)}",
					eventData));
				break;
		}
	}

	async Task Subscribe(Channel<object> channel) {
		List<EventData> batch = [];
		string? currentStreamName = null;
		await foreach (var message in channel.Reader.ReadAllAsync()) {
			switch (message) {
				case ValueTuple<string, EventData>(var streamName, var eventData):
					if (currentStreamName is null || currentStreamName == streamName) {
						currentStreamName = streamName;
						batch.Add(eventData);

						if (batch.Count >= 32) {
							await AppendToStream(currentStreamName, batch);
							batch.Clear();
						}

						continue;
					}

					await AppendToStream(currentStreamName, batch);
					batch.Clear();

					currentStreamName = streamName;
					batch.Add(eventData);
					continue;
				default:
					continue;
			}
		}
	}

}


async Task AppendToStream(string streamName, IEnumerable<EventData> eventData) {
	if (!expectedVersions.TryGetValue(streamName, out var state)) {
		var read = eventStore.ReadStreamAsync(Direction.Backwards, streamName, StreamPosition.End, 1);
		if (await read.ReadState == ReadState.StreamNotFound) {
			state = StreamState.NoStream;
		} else {
			state = await read.Select(m => StreamState.StreamRevision(m.Event.EventNumber.ToUInt64())).SingleAsync();
		}
	}

	await eventStore.AppendToStreamAsync(streamName, state, eventData);
}


EventData GetLinkTo(ResolvedEvent resolvedEvent) {
	var i = resolvedEvent.Event.EventStreamId.Length * 2;
	Span<byte> data = i > 1024 ? new byte[i] : stackalloc byte[i];

	var streamStateLength = Encoding.UTF8.GetBytes(resolvedEvent.Event.EventNumber.ToString(), data);
	data[streamStateLength] = (byte)'@';
	var dataLength = streamStateLength + 1 +
	                 Encoding.UTF8.GetBytes(resolvedEvent.Event.EventStreamId, data[(streamStateLength + 1)..]);

	Span<byte> metadata = stackalloc byte[16];

	BitConverter.TryWriteBytes(metadata, resolvedEvent.Event.Position.CommitPosition);
	BitConverter.TryWriteBytes(metadata[8..], resolvedEvent.Event.Position.PreparePosition);

	return new EventData(resolvedEvent.Event.EventId, "$>", data[..dataLength].ToArray(), metadata.ToArray(),
		contentType: contentType);
}

internal readonly record struct Checkpoints(Position ByType, Position ByCategory) {
	public static readonly Checkpoints Empty = new(Position.End, Position.End);
}

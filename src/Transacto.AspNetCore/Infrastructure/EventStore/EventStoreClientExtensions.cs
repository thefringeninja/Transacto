using EventStore.Client;

namespace Transacto.Infrastructure.EventStore;

public abstract record Expected {
	public static readonly Expected NoStream = new State(StreamState.NoStream);
	public static readonly Expected StreamExists = new State(StreamState.StreamExists);
	public static readonly Expected Any = new State(StreamState.Any);

	public sealed record State(StreamState StreamState) : Expected;

	public sealed record Revision(StreamRevision StreamRevision) : Expected;
}

public static class EventStoreClientExtensions {
	public static Task<IWriteResult> AppendToStreamAsync(this EventStoreClient client, string streamName,
		Expected expected,
		IEnumerable<EventData> eventData,
		Action<EventStoreClientOperationOptions>? configureOperationOptions = null,
		TimeSpan? deadline = null,
		UserCredentials? userCredentials = null,
		CancellationToken cancellationToken = default) => expected switch {
		Expected.State(var state) => client.AppendToStreamAsync(streamName, state, eventData,
			configureOperationOptions, deadline, userCredentials, cancellationToken),
		Expected.Revision(var revision) => client.AppendToStreamAsync(streamName, revision, eventData,
			configureOperationOptions, deadline, userCredentials, cancellationToken),
		_ => throw new ArgumentException($"{expected.GetType()} not expected", nameof(expected))
	};
}

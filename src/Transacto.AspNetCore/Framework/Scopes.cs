using KurrentDB.Client;

namespace Transacto.Framework;

public static class Scopes {
	public readonly record struct EventRecord(string StreamName, StreamPosition StreamPosition,
		string Type, DateTime Created, Position Position, string ContentType) {

		public static EventRecord From(KurrentDB.Client.EventRecord eventRecord) =>
			new(eventRecord.EventStreamId,
				eventRecord.EventNumber,
				eventRecord.EventType,
				eventRecord.Created,
				eventRecord.Position,
				eventRecord.ContentType);
	}
}

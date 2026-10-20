using KurrentDB.Client;

namespace Transacto.Framework.Logging;


public static partial class Scopes {
	[LoggingScope]
	public partial record Projections(string Projection);

	[LoggingScope]
	public partial record Plugins(string Plugin);

	[LoggingScope]
	public partial record ProcessManagers(string ProcessManager, string Plugin, string CheckpointStreamName);

	[LoggingScope]
	public readonly partial record struct EventRecord(string StreamName, StreamPosition StreamPosition,
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

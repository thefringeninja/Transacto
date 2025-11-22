using System.Text.Json;
using KurrentDB.Client;
using Transacto.Framework;

namespace Transacto.Infrastructure.EventStore;

public class EventLogging(
	KurrentDBClient eventStore,
	IMessageTypeMapper messageTypeMapper,
	ILogger<EventLogging> logger) : BackgroundService {
	protected override async Task ExecuteAsync(CancellationToken cancellationToken) {
		if (!logger.IsEnabled(LogLevel.Trace)) {
			return;
		}

		await using var subscription = eventStore.SubscribeToAll(FromAll.Start,
			filterOptions: new(EventTypeFilter.ExcludeSystemEvents()), cancellationToken: cancellationToken);

		await foreach (var message in subscription.Messages.WithCancellation(cancellationToken)) {
			if (message is not StreamMessage.Event(var resolvedEvent)) {
				continue;
			}

			var eventRecord = resolvedEvent.Event;

			var e = messageTypeMapper.TryMap(eventRecord.EventType, out var messageType)
				? JsonSerializer.Deserialize(eventRecord.Data.Span, messageType, TransactoSerializerOptions.Events)
				: null;

			logger.LogTrace("Event {streamName}@{streamNumber} - {eventType} {position} received: {message}",
				eventRecord.EventStreamId, eventRecord.EventNumber, eventRecord.EventType, eventRecord.Position,
				e ?? "N/A");
		}
	}
}

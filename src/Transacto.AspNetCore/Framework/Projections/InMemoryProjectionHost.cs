using System.Text.Json;
using EventStore.Client;
using Polly;
using Projac;

namespace Transacto.Framework.Projections;

public class InMemoryProjectionHost(
	EventStoreClient eventStore,
	IMessageTypeMapper messageTypeMapper,
	InMemoryProjectionDatabase target,
	ILogger<InMemoryProjectionHost> logger,
	params ProjectionHandler<InMemoryProjectionDatabase>[][] projections)
	: BackgroundService {
	private readonly Projector<InMemoryProjectionDatabase> _projector = new(
		EnvelopeResolve.WhenAssignableToHandlerMessageType(projections.SelectMany(_ => _).ToArray()));

	protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
		try {
			await Policy.Handle<Exception>(ex => ex is not OperationCanceledException)
				.WaitAndRetryAsync(5, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
					(ex, delay) => logger.LogInformation(ex,
						"InMemoryProjectionHost encountered an issue. Will retry after {delay}",
						delay))
				.ExecuteAsync(SubscribeInternal, stoppingToken);
		} catch (OperationCanceledException) { }
	}

	private async Task SubscribeInternal(CancellationToken ct) {
		target.Clear();
		await using var subscription = eventStore.SubscribeToAll(FromAll.Start,
			filterOptions: new SubscriptionFilterOptions(EventTypeFilter.ExcludeSystemEvents()), cancellationToken: ct);

		await foreach (var message in subscription.Messages.WithCancellation(ct)) {
			if (message is not StreamMessage.Event(var resolvedEvent)) {
				continue;
			}

			if (!messageTypeMapper.TryMap(resolvedEvent.Event.EventType, out var type)) {
				continue;
			}

			var e = JsonSerializer.Deserialize(resolvedEvent.Event.Data.Span, type!,
				TransactoSerializerOptions.Events)!;

			await _projector.ProjectAsync(target, new Envelope(e, resolvedEvent.OriginalEvent.Position), ct);
		}
	}
}

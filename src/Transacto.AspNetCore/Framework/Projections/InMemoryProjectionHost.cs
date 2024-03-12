using System.Text.Json;
using EventStore.Client;
using Polly;
using Projac;

namespace Transacto.Framework.Projections;

public class InMemoryProjectionHost : IHostedService {
	private readonly EventStoreClient _eventStore;
	private readonly IMessageTypeMapper _messageTypeMapper;
	private readonly InMemoryProjectionDatabase _target;
	private readonly CancellationTokenSource _stopped;

	private readonly Projector<InMemoryProjectionDatabase> _projector;

	public InMemoryProjectionHost(EventStoreClient eventStore, IMessageTypeMapper messageTypeMapper,
		InMemoryProjectionDatabase target, params ProjectionHandler<InMemoryProjectionDatabase>[][] projections) {
		_eventStore = eventStore;
		_messageTypeMapper = messageTypeMapper;
		_target = target;
		_stopped = new CancellationTokenSource();

		_projector = new Projector<InMemoryProjectionDatabase>(
			EnvelopeResolve.WhenAssignableToHandlerMessageType(projections.SelectMany(_ => _).ToArray()));
	}

	public Task StartAsync(CancellationToken cancellationToken) {
		Subscribe();

		return Task.CompletedTask;
	}

	private async void Subscribe() {
		var checkpoint = FromAll.Start;

		await Policy.Handle<Exception>(ex => ex is not OperationCanceledException)
			.WaitAndRetryAsync(5, retryCount => TimeSpan.FromMilliseconds(retryCount * retryCount * 100))
			.ExecuteAsync(Subscribe);
		return;

		async Task Subscribe() {
			await using var subscription = _eventStore.SubscribeToAll(checkpoint,
				filterOptions: new SubscriptionFilterOptions(EventTypeFilter.ExcludeSystemEvents()));

			await foreach (var message in subscription.Messages) {
				if (message is not StreamMessage.Event(var resolvedEvent)) {
					continue;
				}

				if (!_messageTypeMapper.TryMap(resolvedEvent.Event.EventType, out var type)) {
					continue;
				}

				var e = JsonSerializer.Deserialize(resolvedEvent.Event.Data.Span, type!,
					TransactoSerializerOptions.Events)!;

				await _projector.ProjectAsync(_target, new Envelope(e, resolvedEvent.OriginalEvent.Position),
					_stopped.Token);
			}
		}
	}

	public Task StopAsync(CancellationToken cancellationToken) {
		_stopped.Cancel();
		return Task.CompletedTask;
	}
}

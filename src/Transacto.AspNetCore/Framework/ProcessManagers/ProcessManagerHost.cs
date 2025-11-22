using System.Text.Json;
using KurrentDB.Client;
using Polly;

namespace Transacto.Framework.ProcessManagers;

public class ProcessManagerHost(
	KurrentDBClient eventStore,
	IMessageTypeMapper messageTypeMapper,
	string checkpointStreamName,
	ProcessManagerEventHandlerModule processManagerModule,
	ILogger<ProcessManagerHost> logger)
	: BackgroundService {
	private readonly ProcessManagerEventDispatcher _dispatcher = new(processManagerModule);
	private readonly string _name = processManagerModule.GetType().Name;

	protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
		await SetStreamMetadata(stoppingToken);
		await Subscribe(stoppingToken);
	}

	private Task SetStreamMetadata(CancellationToken cancellationToken) =>
		eventStore.SetStreamMetadataAsync(checkpointStreamName, StreamState.NoStream,
			new StreamMetadata(maxCount: 5), options => options.ThrowOnAppendFailure = false,
			cancellationToken: cancellationToken);

	private async Task Subscribe(CancellationToken ct) {
		try {
			await Policy.Handle<Exception>(ex => ex is not OperationCanceledException)
				.WaitAndRetryAsync(5, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
					(ex, delay) => logger.LogInformation(ex,
						"Process manager {processManager} encountered an issue. Will retry after {delay}", _name,
						delay))
				.ExecuteAsync(SubscribeInternal, ct);
		} catch (OperationCanceledException) {
			logger.LogTrace("Operation cancelled.");
		}
	}

	private async Task SubscribeInternal(CancellationToken ct) {
		var checkpoint = await ReadCheckpoint(ct);

		await using var subscription = eventStore.SubscribeToAll(checkpoint.ToEventStoreCheckpoint(),
			filterOptions: new SubscriptionFilterOptions(EventTypeFilter.ExcludeSystemEvents()), cancellationToken: ct);

		await foreach (var message in subscription.Messages.WithCancellation(ct)) {
			logger.LogTrace("Process manager {processManager} saw {message}", _name, message);
			if (message is not StreamMessage.Event (var resolvedEvent)) {
				continue;
			}

			if (!messageTypeMapper.TryMap(resolvedEvent.Event.EventType, out var type)) {
				logger.LogDebug(
					"Process manager {processManager} did not understand {messageType} {streamVersion}@{streamName}",
					_name, resolvedEvent.Event.EventType, resolvedEvent.Event.EventNumber,
					resolvedEvent.Event.EventStreamId);
				continue;
			}

			var e = JsonSerializer.Deserialize(resolvedEvent.Event.Data.Span, type,
				TransactoSerializerOptions.Events) ?? throw new InvalidOperationException();

			logger.LogDebug(
				"Process manager {processManager} handling message {messageType} {streamVersion}@{streamName}",
				_name, resolvedEvent.Event.EventType, resolvedEvent.Event.EventNumber,
				resolvedEvent.Event.EventStreamId);

			checkpoint = await _dispatcher.Handle(e, ct);

			if (checkpoint == Checkpoint.None) {
				logger.LogDebug(
					"Process manager {processManager} did not produce checkpoint when handling message {messageType} {streamVersion}@{streamName}",
					_name, resolvedEvent.Event.EventType, resolvedEvent.Event.EventNumber,
					resolvedEvent.Event.EventStreamId);
				continue;
			}

			await eventStore.AppendToStreamAsync(checkpointStreamName, StreamState.Any, [
				new EventData(Uuid.NewUuid(), "checkpoint", checkpoint.Memory,
					contentType: "application/octet-stream")
			], cancellationToken: ct);

			logger.LogDebug("Process manager {processManager} persisted checkpoint {checkpoint} to {streamName}",
				_name, checkpoint, checkpointStreamName);
		}
	}

	private ValueTask<Checkpoint> ReadCheckpoint(CancellationToken ct) =>
		eventStore.ReadStreamAsync(Direction.Backwards, checkpointStreamName, StreamPosition.End, 1,
				cancellationToken: ct)
			.Messages
			.OfType<StreamMessage.Event>()
			.Select(e => new Checkpoint(e.ResolvedEvent.Event.Data))
			.SingleOrDefaultAsync(ct);
}

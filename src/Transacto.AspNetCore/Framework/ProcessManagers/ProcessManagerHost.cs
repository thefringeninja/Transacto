using System.Text.Json;
using KurrentDB.Client;
using Polly;
using Transacto.Framework.Logging;
using Transacto.Framework.Projections;

namespace Transacto.Framework.ProcessManagers;

public partial class ProcessManagerHost(
	KurrentDBClient eventStore,
	IMessageTypeMapper messageTypeMapper,
	string checkpointStreamName,
	ProcessManagerEventHandlerModule processManagerModule,
	IPluginDescriptor plugin,
	ILogger<ProcessManagerHost> logger)
	: BackgroundService {
	private readonly ProcessManagerEventDispatcher _dispatcher = new(processManagerModule);
	private readonly string _name = processManagerModule.GetType().Name;

	private readonly Scopes.ProcessManagers _loggingScope =
		new Scopes.ProcessManagers(processManagerModule.GetType().Name, plugin.Name, checkpointStreamName);

	protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
		await SetStreamMetadata(stoppingToken);
		await Subscribe(stoppingToken);
	}

	private Task<IWriteResult> SetStreamMetadata(CancellationToken cancellationToken) =>
		eventStore.SetStreamMetadataAsync(checkpointStreamName, StreamState.NoStream,
			new StreamMetadata(maxCount: 5), options => options.ThrowOnAppendFailure = false,
			cancellationToken: cancellationToken);

	private async Task Subscribe(CancellationToken ct) {
		try {
			await Policy.WrapAsync(ProjectionPolicy.CircuitBreaker, ProjectionPolicy.RetryExponentially(LogRetry))
				.ExecuteAsync(SubscribeInternal, ct);
		} catch (OperationCanceledException) {
			LogOperationCancelled(logger);
		}

		return;

		void LogRetry(Exception ex, TimeSpan delay) {
			LogRetryAfterDelay(logger, ex, _name, delay);
		}
	}

	private async Task SubscribeInternal(CancellationToken ct) {
		var checkpoint = await ReadCheckpoint(ct);

		using (logger.BeginScope(_loggingScope)) {
			LogResumingFromCheckpoint(logger, checkpoint);
		}

		await using var subscription = eventStore.SubscribeToAll(checkpoint.ToEventStoreCheckpoint(),
			filterOptions: new SubscriptionFilterOptions(EventTypeFilter.ExcludeSystemEvents()), cancellationToken: ct);

		await foreach (var message in subscription.Messages.WithCancellation(ct)) {
			using var pms = logger.BeginScope(_loggingScope);
			LogSawMessage(logger, message);
			if (message is not StreamMessage.Event (var resolvedEvent)) {
				continue;
			}

			using var ers = logger.BeginScope(Scopes.EventRecord.From(resolvedEvent.Event));

			if (!messageTypeMapper.TryMap(resolvedEvent.Event.EventType, out var type)) {
				LogDidNotUnderstandMessageType(logger);
				continue;
			}

			var e = JsonSerializer.Deserialize(resolvedEvent.Event.Data.Span, type,
				TransactoSerializerOptions.Messages) ?? throw new InvalidOperationException();

			LogDispatchingMessage(logger);

			checkpoint = await _dispatcher.Handle(e, ct);

			if (checkpoint == Checkpoint.None) {
				LogNoCheckpoint(logger);
				continue;
			}

			await eventStore.AppendToStreamAsync(checkpointStreamName, StreamState.Any, [
				new EventData(Uuid.NewUuid(), "checkpoint", checkpoint.Memory,
					contentType: "application/octet-stream")
			], cancellationToken: ct);

			LogPersistedCheckpoint(logger, checkpoint, checkpointStreamName);
		}
	}

	private ValueTask<Checkpoint> ReadCheckpoint(CancellationToken ct) =>
		eventStore.ReadStreamAsync(Direction.Backwards, checkpointStreamName, StreamPosition.End, 1,
				cancellationToken: ct)
			.Messages
			.OfType<StreamMessage.Event>()
			.Select(e => new Checkpoint(e.ResolvedEvent.Event.Data))
			.SingleOrDefaultAsync(ct);

	private readonly record struct ProcessManagerScope(string Name);

	[LoggerMessage(LogLevel.Information,
		"Process manager {processManager} encountered an issue. Will retry after {delay}.")]
	static partial void LogRetryAfterDelay(ILogger<ProcessManagerHost> logger, Exception ex, string processManager,
		TimeSpan delay);

	[LoggerMessage(LogLevel.Trace, "Operation cancelled.")]
	static partial void LogOperationCancelled(ILogger<ProcessManagerHost> logger);

	[LoggerMessage(LogLevel.Information, "Process manager resuming from checkpoint {checkpoint}.")]
	static partial void LogResumingFromCheckpoint(ILogger<ProcessManagerHost> logger, Checkpoint checkpoint);

	[LoggerMessage(LogLevel.Trace, "Process manager saw {message}")]
	static partial void LogSawMessage(ILogger<ProcessManagerHost> logger, StreamMessage message);

	[LoggerMessage(LogLevel.Debug, "Process manager did not understand type.")]
	static partial void LogDidNotUnderstandMessageType(ILogger<ProcessManagerHost> logger);

	[LoggerMessage(LogLevel.Debug, "Process manager dispatching message.")]
	static partial void LogDispatchingMessage(ILogger<ProcessManagerHost> logger);

	[LoggerMessage(LogLevel.Debug, "Process manager did not produce checkpoint when handling message.")]
	static partial void LogNoCheckpoint(ILogger<ProcessManagerHost> logger);

	[LoggerMessage(LogLevel.Debug, "Process manager persisted checkpoint {checkpoint} to {checkpointStreamName}.")]
	static partial void LogPersistedCheckpoint(ILogger<ProcessManagerHost> logger, Checkpoint checkpoint,
		string checkpointStreamName);
}

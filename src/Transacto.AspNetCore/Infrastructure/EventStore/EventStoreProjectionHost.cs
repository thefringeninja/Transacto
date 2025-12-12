using System.Diagnostics;
using System.Text.Json;
using KurrentDB.Client;
using Polly;
using Transacto.Framework;
using Transacto.Framework.ProcessManagers;
using Transacto.Framework.Projections;
using EventTypeFilter = KurrentDB.Client.EventTypeFilter;

namespace Transacto.Infrastructure.EventStore;

public partial class EventStoreProjectionHost<TProcessManager>(
	KurrentDBClient eventStore,
	IMessageTypeMapper messageTypeMapper,
	TProcessManager processManagerModule,
	Func<KurrentDBClient, CancellationToken, ValueTask<FromAll>> readCheckpointAsync,
	ILogger<EventStoreProjectionHost<TProcessManager>> logger)
	: BackgroundService where TProcessManager : ProcessManagerEventHandlerModule {
	private readonly ProcessManagerEventDispatcher _dispatcher = new(processManagerModule);

	private readonly Dictionary<Type, (string streamName, StreamState streamState)> _sequenceNumbers = [];
	private int _subscribed;

	protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
		if (Interlocked.Exchange(ref _subscribed, 1) > 0) {
			logger.LogWarning("Projection host for {name} started more than once.", typeof(TProcessManager).Name);
			return;
		}

		await Subscribe(stoppingToken);
	}

	private async Task Subscribe(CancellationToken ct) {
		try {
			await Policy.WrapAsync(ProjectionPolicy.CircuitBreaker, ProjectionPolicy.RetryExponentially(OnRetry))
				.ExecuteAsync(SubscribeInternal, ct);

			void OnRetry(Exception ex, TimeSpan delay) {
				LogRetryAfterDelay(logger, ex, delay);
			}
		} catch (OperationCanceledException) { }
	}

	private async Task SubscribeInternal(CancellationToken ct) {
		var checkpoint = await readCheckpointAsync(eventStore, ct);

		var stopwatch = Stopwatch.StartNew();

		await foreach (var message in eventStore.SubscribeToAll(checkpoint,
			               filterOptions: new SubscriptionFilterOptions(EventTypeFilter.ExcludeSystemEvents()),
			               cancellationToken: ct).Messages.WithCancellation(ct)) {
			LogSawMessage(logger, message);
			switch (message) {
				case StreamMessage.CaughtUp:
					stopwatch.Stop();
					LogCaughtUp(logger, stopwatch.Elapsed);
					break;
				case StreamMessage.FellBehind:
					LogFellBehind(logger);
					continue;
			}

			if (message is not StreamMessage.Event(var resolvedEvent)) {
				continue;
			}

			using var ers = logger.BeginScope(Scopes.EventRecord.From(resolvedEvent.Event));

			if (!messageTypeMapper.TryMap(resolvedEvent.Event.EventType, out var type)) {
				LogDidNotUnderstandMessageType(logger, resolvedEvent.Event.EventType);
				continue;
			}

			var e = JsonSerializer.Deserialize(resolvedEvent.Event.Data.Span, type, TransactoSerializerOptions.Events)!;

			await _dispatcher.Handle(e, ct);
		}
	}

	private readonly record struct EventStoreProjectionScope(string Name);


	[LoggerMessage(LogLevel.Information, "EventStoreProjectionHost encountered an issue. Will retry after {delay}")]
	static partial void LogRetryAfterDelay(ILogger<EventStoreProjectionHost<TProcessManager>> logger, Exception ex,
		TimeSpan delay);

	[LoggerMessage(LogLevel.Information, "EventStoreProjection resuming from checkpoint {checkpoint}.")]
	static partial void LogResumingFromCheckpoint(ILogger<EventStoreProjectionHost<TProcessManager>> logger,
		FromAll checkpoint);

	[LoggerMessage(LogLevel.Debug, "EventStoreProjection caught up in {elapsed}.")]
	static partial void LogCaughtUp(ILogger<EventStoreProjectionHost<TProcessManager>> logger, TimeSpan elapsed);

	[LoggerMessage(LogLevel.Debug, "EventStoreProjection fell behind; catching up.")]
	static partial void LogFellBehind(ILogger<EventStoreProjectionHost<TProcessManager>> logger);

	[LoggerMessage(LogLevel.Trace, "EventStoreProjection saw {message}")]
	static partial void LogSawMessage(ILogger<EventStoreProjectionHost<TProcessManager>> logger, StreamMessage message);

	[LoggerMessage(LogLevel.Debug, "EventStoreProjection did not understand type {type}.")]
	static partial void LogDidNotUnderstandMessageType(ILogger<EventStoreProjectionHost<TProcessManager>> logger,
		string type);
}

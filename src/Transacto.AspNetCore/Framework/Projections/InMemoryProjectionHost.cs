using System.Diagnostics;
using System.Text.Json;
using KurrentDB.Client;
using Polly;
using Projac;
using EventTypeFilter = KurrentDB.Client.EventTypeFilter;

namespace Transacto.Framework.Projections;

public partial class InMemoryProjectionHost(
	IPluginDescriptor pluginDescriptor,
	KurrentDBClient eventStore,
	IMessageTypeMapper messageTypeMapper,
	InMemoryProjectionDatabase target,
	ILogger<InMemoryProjectionHost> logger,
	params IEnumerable<ProjectionHandler<InMemoryProjectionDatabase>[]> projections)
	: BackgroundService {
	private readonly Projector<InMemoryProjectionDatabase> _projector = new(
		EnvelopeResolve.WhenAssignableToHandlerMessageType(projections.SelectMany(_ => _).ToArray()));

	protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
		try {
			await Policy.Handle<Exception>(ex => ex is not OperationCanceledException)
				.WaitAndRetryAsync(5, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
					(ex, delay) => LogRetryAfterDelay(logger, ex, delay))
				.ExecuteAsync(SubscribeInternal, stoppingToken);
		} catch (OperationCanceledException) {
		}
	}

	private async Task SubscribeInternal(CancellationToken ct) {
		target.Clear();

		var scope = new InMemoryProjectionScope(pluginDescriptor.Name);
		
		using (logger.BeginScope(scope)) {
			LogResumingFromCheckpoint(logger, FromAll.Start);
		}

		var stopwatch = Stopwatch.StartNew();
		await using var subscription = eventStore.SubscribeToAll(FromAll.Start,
			filterOptions: new SubscriptionFilterOptions(EventTypeFilter.ExcludeSystemEvents()), cancellationToken: ct);

		await foreach (var message in subscription.Messages.WithCancellation(ct)) {
			LogSawMessage(logger, message);
			if (message is StreamMessage.CaughtUp) {
				stopwatch.Stop();
				LogCaughtUp(logger, stopwatch.Elapsed);
			}
			if (message is not StreamMessage.Event(var resolvedEvent)) {
				continue;
			}
			
			using var ers = logger.BeginScope(Scopes.EventRecord.From(resolvedEvent.Event));

			if (!messageTypeMapper.TryMap(resolvedEvent.Event.EventType, out var type)) {
				LogDidNotUnderstandMessageType(logger, resolvedEvent.Event.EventType);
				continue;
			}

			var e = JsonSerializer.Deserialize(resolvedEvent.Event.Data.Span, type!,
				TransactoSerializerOptions.Events)!;

			await _projector.ProjectAsync(target, new Envelope(e, resolvedEvent.OriginalEvent.Position), ct);
		}
	}
	
	private readonly record struct InMemoryProjectionScope(string Name);


	[LoggerMessage(LogLevel.Information, "InMemoryProjectionHost encountered an issue. Will retry after {delay}")]
	static partial void LogRetryAfterDelay(ILogger<InMemoryProjectionHost> logger, Exception ex, TimeSpan delay);
	
	[LoggerMessage(LogLevel.Information, "InMemoryProjection resuming from checkpoint {checkpoint}.")]
	static partial void LogResumingFromCheckpoint(ILogger<InMemoryProjectionHost> logger, FromAll checkpoint);

	[LoggerMessage(LogLevel.Information, "InMemoryProjection caught up in {elapsed}.")]
	static partial void LogCaughtUp(ILogger<InMemoryProjectionHost> logger, TimeSpan elapsed);

	[LoggerMessage(LogLevel.Trace, "InMemoryProjection saw {message}")]
	static partial void LogSawMessage(ILogger<InMemoryProjectionHost> logger, StreamMessage message);

	[LoggerMessage(LogLevel.Debug, "InMemoryProjection did not understand type {type}.")]
	static partial void LogDidNotUnderstandMessageType(ILogger<InMemoryProjectionHost> logger, string type);


}

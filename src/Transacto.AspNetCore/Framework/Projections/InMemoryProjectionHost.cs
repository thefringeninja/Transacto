using System.Collections.Concurrent;
using System.Collections.ObjectModel;
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
	params IEnumerable<InMemoryProjection> projections)
	: BackgroundService, IProjectionHostStatus, IProjectionsControl {
	private readonly Projector<InMemoryProjectionDatabase> _projector = new(
		EnvelopeResolve.WhenAssignableToHandlerMessageType(projections.SelectMany(_ => _).ToArray()));

	private readonly InMemoryProjectionCollection _projections = [..projections];

	private readonly ConcurrentDictionary<string, ProjectionState> _state = [];
	private KurrentDBClient.StreamSubscriptionResult? _currentSubscription;

	public IReadOnlyList<ProjectionState> State => _state.Values.ToList();

	public bool TryGetState(string name, out ProjectionState state) =>
		_state.TryGetValue(name, out state);

	public async Task Delete(string name) {
		if (_currentSubscription is not { } subscription) {
			return;
		}

		await subscription.DisposeAsync();
	}


	protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
		try {
			await Policy.WrapAsync(ProjectionPolicy.CircuitBreaker, ProjectionPolicy.RetryExponentially(OnRetry))
				.ExecuteAsync(SubscribeInternal, stoppingToken);

			void OnRetry(Exception ex, TimeSpan delay) {
				_state.UpdateAll(static (state, ex) => state with {
					Status = ProjectionStatus.Faulted,
					ErrorMessage = ex.Message
				}, ex);
				LogRetryAfterDelay(logger, ex, delay);
			}
		} catch (OperationCanceledException) {
		}
	}

	private async Task SubscribeInternal(CancellationToken ct) {
		target.Clear();

		var scope = new InMemoryProjectionScope(pluginDescriptor.Name);

		foreach (var projectionHandler in _projections) {
			var key = $"{projectionHandler.Name}-Projection";
			_state.TryAdd(key, ProjectionState.Named(key));
		}

		using (logger.BeginScope(scope)) {
			LogResumingFromCheckpoint(logger, FromAll.Start);
		}

		await FellBehind(ct);

		var stopwatch = Stopwatch.StartNew();

		_currentSubscription = eventStore.SubscribeToAll(FromAll.Start,
			filterOptions: new SubscriptionFilterOptions(EventTypeFilter.ExcludeSystemEvents()), cancellationToken: ct);

		await foreach (var message in _currentSubscription.Messages.WithCancellation(ct)) {
			LogSawMessage(logger, message);
			switch (message) {
				case StreamMessage.CaughtUp:
					stopwatch.Stop();
					LogCaughtUp(logger, stopwatch.Elapsed);
					_state.UpdateAll(value => value with { Status = ProjectionStatus.Live });
					break;
				case StreamMessage.FellBehind:
					await FellBehind(ct);

					continue;
			}

			if (message is not StreamMessage.Event(var resolvedEvent)) {
				continue;
			}

			_state.UpdateAll((value, position) => value
				.End(FromAll.After(position))
				.Checkpoint(FromAll.After(position)), resolvedEvent.Event.Position);

			using var ers = logger.BeginScope(Scopes.EventRecord.From(resolvedEvent.Event));

			if (!messageTypeMapper.TryMap(resolvedEvent.Event.EventType, out var type)) {
				LogDidNotUnderstandMessageType(logger, resolvedEvent.Event.EventType);
				continue;
			}

			var e = JsonSerializer.Deserialize(resolvedEvent.Event.Data.Span, type, TransactoSerializerOptions.Events)!;

			await _projector.ProjectAsync(target, new Envelope(e, resolvedEvent.OriginalEvent.Position), ct);
		}
	}

	private async Task FellBehind(CancellationToken ct) {
		var last = await eventStore.ReadAllAsync(Direction.Backwards, Position.End, 1)
			.Select(re => re.Event.Position)
			.FirstOrDefaultAsync(ct);

		_state.UpdateAll(
			static (value, position) =>
				value.End(FromAll.After(position)) with { Status = ProjectionStatus.CatchingUp },
			last);
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

	private class InMemoryProjectionCollection : KeyedCollection<string, InMemoryProjection> {
		protected override string GetKeyForItem(InMemoryProjection item) => item.Name;
	}
}

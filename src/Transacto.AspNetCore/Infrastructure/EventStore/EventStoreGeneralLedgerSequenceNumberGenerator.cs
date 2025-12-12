using System.Diagnostics;
using System.Text.Json;
using System.Threading.Channels;
using Inflector;
using KurrentDB.Client;
using Polly;
using Transacto.Domain;
using Transacto.Framework;
using Transacto.Framework.ProcessManagers;
using Transacto.Framework.Projections;
using Transacto.Messages;
using EventTypeFilter = KurrentDB.Client.EventTypeFilter;

namespace Transacto.Infrastructure.EventStore;

public partial class EventStoreProjectionHost<TProcessManager>(
	KurrentDBClient eventStore,
	IMessageTypeMapper messageTypeMapper,
	TProcessManager processManagerModule,
	Func<KurrentDBClient, CancellationToken, ValueTask<FromAll>> readCheckpointAsync,
	ILogger<EventStoreProjectionHost<TProcessManager>> logger) : BackgroundService where TProcessManager : ProcessManagerEventHandlerModule {
	private readonly ProcessManagerEventDispatcher _dispatcher = new(processManagerModule);

	protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
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
	static partial void LogRetryAfterDelay(ILogger<EventStoreProjectionHost<TProcessManager>> logger, Exception ex, TimeSpan delay);

	[LoggerMessage(LogLevel.Information, "EventStoreProjection resuming from checkpoint {checkpoint}.")]
	static partial void LogResumingFromCheckpoint(ILogger<EventStoreProjectionHost<TProcessManager>> logger, FromAll checkpoint);

	[LoggerMessage(LogLevel.Information, "EventStoreProjection caught up in {elapsed}.")]
	static partial void LogCaughtUp(ILogger<EventStoreProjectionHost<TProcessManager>> logger, TimeSpan elapsed);

	[LoggerMessage(LogLevel.Trace, "EventStoreProjection saw {message}")]
	static partial void LogSawMessage(ILogger<EventStoreProjectionHost<TProcessManager>> logger, StreamMessage message);

	[LoggerMessage(LogLevel.Debug, "EventStoreProjection did not understand type {type}.")]
	static partial void LogDidNotUnderstandMessageType(ILogger<EventStoreProjectionHost<TProcessManager>> logger, string type);

}

public class EventStoreSequenceNumberGenerator<T>(
	KurrentDBClient client,
	IMessageTypeMapper messageTypeMapper,
	int year)
	: ISequenceNumberGenerator, IDisposable where T : IBusinessTransaction {
	private readonly string _streamName = $"{typeof(T).Name.Underscore().Camelize()}-{year}";
	private readonly CancellationTokenSource _cts = new();

	private Channel<ulong> channel {
		get {
			return field ??= CreateChannel();

			Channel<ulong> CreateChannel() {
				var channel = Channel.CreateBounded<ulong>(new BoundedChannelOptions(1024));

				_ = Subscribe(channel);

				return channel;
			}
		}
	}


	async Task Subscribe(Channel<ulong> channel) {
		var checkpoint = FromStream.Start;
		await foreach (var message in client.ReadStreamAsync(Direction.Backwards, _streamName,
			               StreamPosition.End, 1, cancellationToken: _cts.Token).Messages) {
			if (message is not StreamMessage.Event (var resolvedEvent)) {
				continue;
			}

			checkpoint = FromStream.After(resolvedEvent.Event.EventNumber);
		}

		await channel.Writer.WriteAsync(ulong.MaxValue, _cts.Token);

		await foreach (var message in client.SubscribeToStream(_streamName, checkpoint).Messages) {
			if (message is not StreamMessage.Event (var resolvedEvent) ||
			    !messageTypeMapper.TryMap(resolvedEvent.Event.EventType, out var type) ||
			    type != typeof(GeneralLedgerEntryNumberAllocated)) {
				continue;
			}

			await channel.Writer.WriteAsync(resolvedEvent.Event.EventNumber.ToUInt64());
		}
	}

	public async ValueTask<GeneralLedgerEntrySequenceNumber> NextAsync(
		GeneralLedgerEntryIdentifier generalLedgerEntryIdentifier, CancellationToken cancellationToken = default) {
		var next = await channel.Reader.ReadAsync(cancellationToken);

		var assigned = new GeneralLedgerEntryNumberAllocated {
			GeneralLedgerEntryId = generalLedgerEntryIdentifier.ToGuid()
		};

		var eventData = new EventData(Uuid.NewUuid(), messageTypeMapper.Map(typeof(GeneralLedgerEntryNumberAllocated)),
			JsonSerializer.SerializeToUtf8Bytes(assigned, TransactoSerializerOptions.Events));

		var streamState = next == ulong.MaxValue ? StreamState.NoStream : StreamState.StreamRevision(next);

		var result = await client.AppendToStreamAsync(_streamName, streamState, [eventData],
			cancellationToken: cancellationToken);

		return !result.NextExpectedStreamState.HasPosition
			? throw new InvalidOperationException()
			: new GeneralLedgerEntrySequenceNumber(unchecked((int)result.NextExpectedStreamState.ToInt64()));
	}

	public void Dispose() {
		channel.Writer.TryComplete();
		_cts.Dispose();
	}
}

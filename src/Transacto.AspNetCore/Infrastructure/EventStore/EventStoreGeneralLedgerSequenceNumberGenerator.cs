using System.Text.Json;
using System.Threading.Channels;
using DeterministicGuids;
using Inflector;
using KurrentDB.Client;
using SqlStreamStore.Imports.AsyncEx.Nito.AsyncEx.Coordination;
using Transacto.Domain;
using Transacto.Framework;
using Transacto.Messages;

namespace Transacto.Infrastructure.EventStore;

public class EventStoreSequenceNumberGenerator : IGeneralLedgerEntrySequenceNumberGenerator, IDisposable,
	IAsyncDisposable {
	private static readonly Guid s_namespace = Guid.Parse("51aadc0e-1045-4aab-9845-4cfbce83e7b3");

	private readonly string _streamNamePrefix;

	// Guards creation of per-year state. Each year is read from its stream once, before its first number is handed out.
	private readonly SemaphoreSlim _yearsGate = new(1, 1);
	private readonly Dictionary<int, YearState> _years = [];

	private readonly Channel<object> _channel = Channel.CreateUnbounded<object>(new() {
		SingleReader = true,
		SingleWriter = false
	});

	private readonly KurrentDBClient _client;
	private readonly IMessageTypeMapper _messageTypeMapper;
	private readonly ILogger<EventStoreSequenceNumberGenerator> _logger;
	private readonly CancellationTokenSource _cts = new();
	private readonly Dictionary<int, List<EventData>> _batches = [];
	private readonly Task _subscription;
	private readonly AsyncAutoResetEvent _activity = new();
	private readonly TimeSpan _debounce = TimeSpan.FromMilliseconds(200);

	public EventStoreSequenceNumberGenerator(KurrentDBClient client,
		IMessageTypeMapper messageTypeMapper,
		Type businessTransactionType,
		ILogger<EventStoreSequenceNumberGenerator> logger) {
		_client = client;
		_messageTypeMapper = messageTypeMapper;
		_logger = logger;
		_streamNamePrefix = $"{businessTransactionType.Name.Underscore().Camelize()}SequenceNumbers-";
		_subscription = Task.WhenAll(Subscribe(), DebounceFlushLoop());
	}

	private async Task Subscribe() {
		await foreach (var message in _channel.Reader.ReadAllAsync(_cts.Token)) {
			switch (message) {
				case ValueTuple<int, EventData>(var year, var eventData): {
					if (!_batches.TryGetValue(year, out var batch)) {
						batch = _batches[year] = new List<EventData>(32);
					}

					batch.Add(eventData);

					if (batch.Count < 32) {
						break;
					}

					await FlushBatch(year, batch);

					break;
				}
				case long: {
					foreach (var year in _batches.Keys) {
						var batch = _batches[year];

						await FlushBatch(year, batch);
					}

					break;
				}
			}

			_activity.Set();
		}
	}

	private async Task DebounceFlushLoop() {
		while (!_cts.Token.IsCancellationRequested) {
			// 1. Sleep until *some event arrives*
			await _activity.WaitAsync();

			// 2. Now wait 200 ms of silence
			while (true) {
				var delayTask = Task.Delay(_debounce, _cts.Token);
				var activityTask = _activity.WaitAsync();

				var completed = await Task.WhenAny(delayTask, activityTask);

				if (completed == delayTask) {
					// Silence for 200 ms -> flush
					await _channel.Writer.WriteAsync(long.MinValue);
					break; // go back to waiting for new activity
				}

				// else: activity happened -> restart 200ms timer
			}
		}
	}

	private async Task FlushBatch(int year, List<EventData> batch) {
		if (batch.Count == 0) {
			if (_logger.IsEnabled(LogLevel.Debug)) {
				_logger.LogDebug("Flushing batch for year {year} empty", year);
			}

			return;
		}

		try {
			var state = await GetYearStateAsync(year, _cts.Token);

			// A redelivered entry is allocated the same deterministic event id, so drop duplicates from the batch.
			var events = batch.DistinctBy(e => e.EventId.ToString()).ToList();

			try {
				await _client.AppendToStreamAsync(StreamNameFor(year), state.LastRevision, events,
					cancellationToken: _cts.Token);

				state.LastRevision = Advance(state.LastRevision, events.Count);
			} catch (WrongExpectedVersionException) {
				// The stream moved underneath us (for example, a batch was partly written before a restart).
				_logger.LogWarning("Batch for year {year} conflicted with the stream; resyncing", year);

				await ResyncAsync(year, state);
				await AppendOneByOneAsync(year, state, events);
			}

			batch.Clear();

			if (_logger.IsEnabled(LogLevel.Debug)) {
				_logger.LogDebug("Flushed batch for year {year} with count {count} ", year, events.Count);
			}
		} catch (Exception ex) {
			_logger.LogError(ex, "Flushing batch for year {year} with count {count} failed", year, batch.Count);
		}
	}

	private async Task AppendOneByOneAsync(int year, YearState state, List<EventData> events) {
		foreach (var eventData in events) {
			try {
				await _client.AppendToStreamAsync(StreamNameFor(year), state.LastRevision, [eventData],
					cancellationToken: _cts.Token);

				state.LastRevision = Advance(state.LastRevision, 1);
			} catch (WrongExpectedVersionException) {
				// This allocation is already in the stream (a redelivery), so skip it and resync.
				_logger.LogWarning("Allocation {eventId} for year {year} is already in the stream; skipping", eventData.EventId,
					year);

				await ResyncAsync(year, state);
			}
		}
	}

	private async Task ResyncAsync(int year, YearState state) {
		var (revision, number) = await ReadLastAsync(StreamNameFor(year), _cts.Token);

		state.LastRevision = revision;

		lock (state.Gate) {
			state.LastNumber = Math.Max(state.LastNumber, number);
		}
	}

	private async ValueTask<YearState> GetYearStateAsync(int year, CancellationToken cancellationToken) {
		await _yearsGate.WaitAsync(cancellationToken);

		try {
			if (!_years.TryGetValue(year, out var state)) {
				var (revision, number) = await ReadLastAsync(StreamNameFor(year), cancellationToken);

				state = _years[year] = new YearState(revision, number);
			}

			return state;
		} finally {
			_yearsGate.Release();
		}
	}

	/// <summary>
	/// Reads the last allocation in a year's stream: its revision, and the number it allocated.
	/// The number comes from the payload, not the revision, so gaps can never cause a collision.
	/// </summary>
	private async Task<(StreamState Revision, int Number)> ReadLastAsync(string streamName,
		CancellationToken cancellationToken) {
		var readResult = _client.ReadStreamAsync(Direction.Backwards, streamName, StreamPosition.End, 1,
			cancellationToken: cancellationToken);

		if (await readResult.ReadState == ReadState.StreamNotFound) {
			return (StreamState.NoStream, 0);
		}

		await foreach (var resolvedEvent in readResult.WithCancellation(cancellationToken)) {
			var allocated = JsonSerializer.Deserialize<GeneralLedgerEntryNumberAllocated>(
				resolvedEvent.Event.Data.Span, TransactoSerializerOptions.Messages);

			return (StreamState.StreamRevision(resolvedEvent.Event.EventNumber),
				allocated?.GeneralLedgerEntrySequenceNumber ?? 0);
		}

		return (StreamState.NoStream, 0);
	}

	public async ValueTask<GeneralLedgerEntrySequenceNumber> NextAsync(
		GeneralLedgerEntry generalLedgerEntry, CancellationToken cancellationToken = default) {
		var year = generalLedgerEntry.AccountingPeriod.Year;

		var state = await GetYearStateAsync(year, cancellationToken);

		int next;
		lock (state.Gate) {
			next = ++state.LastNumber;
		}

		var assigned = new GeneralLedgerEntryNumberAllocated {
			GeneralLedgerEntryId = generalLedgerEntry.Identifier.ToGuid(),
			GeneralLedgerEntrySequenceNumber = next
		};

		var eventId = Uuid.FromGuid(DeterministicGuid.Create(s_namespace, generalLedgerEntry.Identifier.ToString(),
			DeterministicGuid.Version.SHA256));

		var eventData = new EventData(eventId, _messageTypeMapper.Map(typeof(GeneralLedgerEntryNumberAllocated)),
			JsonSerializer.SerializeToUtf8Bytes(assigned, TransactoSerializerOptions.Messages));

		await _channel.Writer.WriteAsync((year, eventData), cancellationToken);

		return new GeneralLedgerEntrySequenceNumber(next);
	}

	private string StreamNameFor(int year) => $"{_streamNamePrefix}{year}";

	// The revision of the last event in the year's stream, which is the expected version for the next append.
	private static StreamState Advance(StreamState revision, int count) => revision == StreamState.NoStream
		? StreamState.StreamRevision((ulong)(count - 1))
		: StreamState.StreamRevision((ulong)(revision.ToInt64() + count));

	private sealed class YearState(StreamState revision, int number) {
		public readonly object Gate = new();

		// Owned by the flush loop once the state exists.
		public StreamState LastRevision = revision;

		// Highest number handed out for the year. Guarded by Gate.
		public int LastNumber = number;
	}

	public void Dispose() {
		_cts.Cancel();
		_cts.Dispose();
	}

	public async ValueTask DisposeAsync() {
		await _cts.CancelAsync();
		await _subscription.ContinueWith(task => {
			if (!task.IsFaulted) return;
			_logger.LogError(task.Exception!.Flatten(), "Woops");
		});
		_cts.Dispose();
	}
}

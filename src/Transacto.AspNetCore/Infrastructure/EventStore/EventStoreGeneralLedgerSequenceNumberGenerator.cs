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

	private readonly Dictionary<int, StreamState> _streamStates = [];

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
	private TaskCompletionSource _timerReset = new(TaskCreationOptions.RunContinuationsAsynchronously);
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
		try {
			if (_logger.IsEnabled(LogLevel.Trace)) {
				_logger.LogTrace("Flushing batch for year {year} with count {count}", year, batch.Count);
			}

			if (batch.Count == 0) {
				if (_logger.IsEnabled(LogLevel.Debug)) {
					_logger.LogDebug("Flushing batch for year {year} empty", year);
				}

				return;
			}

			var streamName = $"{_streamNamePrefix}{year}";
			var state = _streamStates[year] == StreamState.NoStream
				? StreamState.NoStream
				: StreamState.StreamRevision((ulong)(_streamStates[year].ToInt64() - batch.Count));

			await _client.AppendToStreamAsync(streamName, state, batch, cancellationToken: _cts.Token);

			if (_logger.IsEnabled(LogLevel.Debug)) {
				_logger.LogDebug("Flushed batch for year {year} with count {count} ", year, batch.Count);
			}

			batch.Clear();
		} catch (Exception ex) {
			_logger.LogError(ex, "Flushing batch for year {year} with count {count} failed", year, batch.Count);
		}
	}

	public async ValueTask<GeneralLedgerEntrySequenceNumber> NextAsync(
		GeneralLedgerEntry generalLedgerEntry, CancellationToken cancellationToken = default) {
		var year = generalLedgerEntry.AccountingPeriod.Year;

		var streamName = $"{_streamNamePrefix}{year}";

		if (!_streamStates.TryGetValue(year, out var state)) {
			var readResult = _client.ReadStreamAsync(Direction.Backwards, streamName, StreamPosition.End, 1,
				cancellationToken: cancellationToken);
			if (await readResult.ReadState == ReadState.StreamNotFound) {
				state = StreamState.NoStream;
			} else {
				state = await readResult
					.Select(re => new StreamState?(StreamState.StreamRevision(re.Event.EventNumber)))
					.SingleOrDefaultAsync(cancellationToken) ?? StreamState.NoStream;
			}
		}

		_streamStates[year] = state == StreamState.NoStream ? StreamState.StreamRevision(0) : state.Next();

		var next = (int)_streamStates[year].Next().ToInt64(); // 2^32 transactions per year is unlikely

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

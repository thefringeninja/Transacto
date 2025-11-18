using System.Data.SQLite;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Channels;
using EventStore.Client;
using Polly;
using Projac.Sql;
using Projac.Sql.Executors;
using Projac.SQLite;
using Transacto.Framework;
using Transacto.Framework.Projections;
using EventTypeFilter = EventStore.Client.EventTypeFilter;
using Timer = System.Threading.Timer;

namespace Transacto.Infrastructure.SQLite;

public class SQLiteProjectionHost(
	EventStoreClient eventStore,
	IMessageTypeMapper messageTypeMap,
	Func<SQLiteConnection> connectionFactory,
	SQLiteProjection projection,
	ILogger<SQLiteProjectionHost> logger)
	: IHostedService {
	private readonly CancellationTokenSource _stopped = new();
	private readonly SqlProjectionHandlerResolver _resolver = Resolve.WhenEqualToHandlerMessageType(projection);

	private static readonly SQLiteSyntax Sql = new();

	public Task StartAsync(CancellationToken cancellationToken) {
		CreateSchema();

		Subscribe();

		return Task.CompletedTask;
	}

	private async void Subscribe() {
		try {
			await Policy.Handle<Exception>(ex => ex is not OperationCanceledException)
				.WaitAndRetryAsync(5, retryCount => TimeSpan.FromMilliseconds(retryCount * retryCount * 100))
				.ExecuteAsync(Subscribe);
		} catch (OperationCanceledException) { }

		return;

		async Task Subscribe() {
			var checkpoint = ReadCheckpoint();

			var stopwatch = Stopwatch.StartNew();

			logger.LogInformation("Projection {projection} subscribing from {checkpoint}", projection.Name,
				checkpoint);

			await using var subscription = eventStore.SubscribeToAll(checkpoint,
				filterOptions: new(EventTypeFilter.ExcludeSystemEvents()));

			var batch = new List<ResolvedEvent>(110);

			await foreach (var message in subscription.Batch(100, TimeSpan.FromMilliseconds(20), _stopped.Token)) {
				switch (message) {
					case SubscriptionControlMessage.SubscriptionMessage (var streamMessage):
						if (streamMessage is StreamMessage.CaughtUp) {
							stopwatch.Stop();
							logger.LogInformation("Projection {projection} caught up. Elapsed {elapsed}",
								projection.Name, stopwatch.Elapsed);
							continue;
						}

						if (streamMessage is not StreamMessage.Event(var resolvedEvent)) {
							continue;
						}

						if (!messageTypeMap.TryMap(resolvedEvent.Event.EventType, out _)) {
							continue;
						}

						batch.Add(resolvedEvent);
						break;
					case SubscriptionControlMessage.Flush: {
						if (batch.Count == 0) {
							break;
						}

						await using var connection = connectionFactory();
						connection.Open();
						await using var transaction = connection.BeginTransaction();
						var sqlProjector = new SqlProjector(
							_resolver, new ConnectedTransactionalSqlCommandExecutor(transaction));
						sqlProjector.Project(batch.Select(re => JsonSerializer.Deserialize(re.Event.Data.Span,
							messageTypeMap.Map(re.Event.EventType),
							TransactoSerializerOptions.Events) ?? throw new InvalidOperationException()));
						WriteCheckpoint(transaction, batch[^1].Event.Position);
						transaction.Commit();
						batch.Clear();
						break;
					}
				}
			}
		}
	}

	private void CreateSchema() {
		var executor = new SQLiteExecutor(connectionFactory);

		executor.ExecuteNonQuery(Sql.NonQueryStatement(SQLiteScripts.CreateSchema));

		new SqlProjector(_resolver, executor).Project(new CreateSchema());

		logger.LogInformation("Schema created {projection}", projection);
	}

	public Task StopAsync(CancellationToken cancellationToken) {
		_stopped.Cancel();
		return Task.CompletedTask;
	}

	private FromAll ReadCheckpoint() {
		using var connection = connectionFactory();
		connection.Open();

		var statement = Sql.QueryStatement(SQLiteScripts.ReadCheckpoint);

		using var command = new SQLiteCommand(statement.Text, connection);

		using var reader = command.ExecuteReader();
		return !reader.Read()
			? FromAll.Start
			: FromAll.After(new Position(unchecked((ulong)reader.GetInt64(0)), unchecked((ulong)reader.GetInt64(1))));
	}

	private static void WriteCheckpoint(SQLiteTransaction transaction, Position position) {
		var statement = Sql.QueryStatement(SQLiteScripts.WriteCheckpoint);

		using var command = unchecked(new SQLiteCommand(statement.Text, transaction.Connection, transaction) {
			Parameters = {
				Sql.Int64((long)position.CommitPosition).ToDbParameter("commit"),
				Sql.Int64((long)position.PreparePosition).ToDbParameter("prepare"),
			}
		});

		command.ExecuteNonQuery();
	}
}

internal static class StreamSubscriptionResultExtensions {
	public static IAsyncEnumerable<SubscriptionControlMessage> Batch(
		this EventStoreClient.StreamSubscriptionResult subscription,
		int batchSize, TimeSpan period, CancellationToken cancellationToken) {
		var channel = Channel.CreateBounded<SubscriptionControlMessage>(new BoundedChannelOptions(batchSize) {
			FullMode = BoundedChannelFullMode.Wait,
			SingleWriter = false,
			SingleReader = true
		});

		var timer = new Timer(state => {
			var channel = (Channel<SubscriptionControlMessage>)state!;
			channel.Writer.TryWrite(SubscriptionControlMessage.Flush.Instance);
		}, channel, period, period);

		Produce();

		return channel.Reader.ReadAllAsync(cancellationToken);

		async void Produce() {
			int count = 0;
			try {
				await foreach (var message in subscription.Messages.WithCancellation(cancellationToken)) {
					timer.Change(period, period);
					await channel.Writer.WriteAsync(new SubscriptionControlMessage.SubscriptionMessage(message),
						cancellationToken);
					if (count++ >= batchSize) {
						count = 0;
						await channel.Writer.WriteAsync(SubscriptionControlMessage.Flush.Instance, cancellationToken);
					}
				}
			} catch (Exception ex) {
				channel.Writer.TryComplete(ex);
				return;
			}

			channel.Writer.Complete();
		}
	}
}

internal abstract record SubscriptionControlMessage {
	public sealed record Flush : SubscriptionControlMessage {
		public static readonly Flush Instance = new();
	}

	public sealed record SubscriptionMessage(StreamMessage StreamMessage) : SubscriptionControlMessage;
}

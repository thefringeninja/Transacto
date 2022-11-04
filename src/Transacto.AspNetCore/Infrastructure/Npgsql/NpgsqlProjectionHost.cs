using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EventStore.Client;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Projac.Sql;
using Projac.Sql.Executors;
using Serilog;
using Transacto.Framework;
using Transacto.Framework.Projections;

namespace Transacto.Infrastructure.Npgsql; 

public class NpgsqlProjectionHost : IHostedService {
	private readonly EventStoreClient _eventStore;
	private readonly IMessageTypeMapper _messageTypeMap;
	private readonly Func<NpgsqlConnection> _connectionFactory;
	private readonly NpgsqlProjection[] _projections;
	private readonly CancellationTokenSource _stopped;

	private int _retryCount;
	private int _subscribed;
	private StreamSubscription? _subscription;
	private CancellationTokenRegistration? _stoppedRegistration;

	public NpgsqlProjectionHost(EventStoreClient eventStore, IMessageTypeMapper messageTypeMap,
		Func<NpgsqlConnection> connectionFactory, params NpgsqlProjection[] projections) {
		_eventStore = eventStore;
		_messageTypeMap = messageTypeMap;
		_connectionFactory = connectionFactory;
		_projections = projections;
		_stopped = new CancellationTokenSource();

		_retryCount = 0;
		_subscribed = 0;
		_subscription = null;
		_stoppedRegistration = null;
	}

	public async Task StartAsync(CancellationToken cancellationToken) {
		if (_projections.Length == 0) {
			return;
		}

		await CreateSchema(cancellationToken);

		await Subscribe(cancellationToken);
	}

	private async Task Subscribe(CancellationToken cancellationToken) {
		if (Interlocked.CompareExchange(ref _subscribed, 1, 0) == 1) {
			return;
		}

		var registration = _stoppedRegistration;
		if (registration != null) {
			await registration.Value.DisposeAsync();
		}

		var projectors = await ReadCheckpoints();
		var projector = new CheckpointAwareProjector(_connectionFactory, _messageTypeMap, projectors);
		var checkpoint = projectors.Select(p => p.Checkpoint).Min();

		Interlocked.Exchange(ref _subscription, await _eventStore.SubscribeToAllAsync(checkpoint,
			projector.ProjectAsync,
			subscriptionDropped: (_, reason, ex) => {
				if (reason == SubscriptionDroppedReason.Disposed) {
					return;
				}

				if (Interlocked.Increment(ref _retryCount) == 5) {
					Log.Error(ex, "Subscription dropped: {reason}", reason);
					return;
				}

				Log.Warning(ex, "Subscription dropped: {reason}; resubscribing...", reason);
				Interlocked.Exchange(ref _subscribed, 0);
				Task.Run(() => Subscribe(cancellationToken), cancellationToken);
			},
			filterOptions: new SubscriptionFilterOptions(EventTypeFilter.ExcludeSystemEvents()),
			userCredentials: new UserCredentials("admin", "changeit"),
			cancellationToken: _stopped.Token));

		_stoppedRegistration = _stopped.Token.Register(_subscription.Dispose);

		async ValueTask<Projector[]> ReadCheckpoints() {
			await using var connection = _connectionFactory();
			await connection.OpenAsync(cancellationToken);
			return await Task.WhenAll(Array.ConvertAll(_projections,
				async projection => new Projector(projection,
					await projection.ReadCheckpoint(connection, cancellationToken))));
		}
	}

	private Task CreateSchema(CancellationToken cancellationToken) =>
		Task.WhenAll(_projections.Select(p =>
			new AsyncSqlProjector(Resolve.WhenEqualToHandlerMessageType(p),
				new NpgsqlExecutor(_connectionFactory)).ProjectAsync(new CreateSchema(), cancellationToken)));

	public Task StopAsync(CancellationToken cancellationToken) {
		_stopped.Cancel();
		_stoppedRegistration?.Dispose();
		return Task.CompletedTask;
	}

	private record Projector(NpgsqlProjection Projection, Position Checkpoint) {
		public SqlProjectionHandlerResolver Resolver { get; } = Resolve.WhenEqualToHandlerMessageType(Projection);
	}

	private class CheckpointAwareProjector {
		private readonly Func<NpgsqlConnection> _connectionFactory;
		private readonly IMessageTypeMapper _messageTypeMapper;

		private readonly Projector[] _projectors;

		public CheckpointAwareProjector(Func<NpgsqlConnection> connectionFactory,
			IMessageTypeMapper messageTypeMapper, Projector[] projectors) {
			_connectionFactory = connectionFactory;
			_messageTypeMapper = messageTypeMapper;
			_projectors = projectors;
		}

		public Task ProjectAsync(StreamSubscription subscription, ResolvedEvent e,
			CancellationToken cancellationToken) {
			if (!_messageTypeMapper.TryMap(e.Event.EventType, out var type)) {
				return Task.CompletedTask;
			}

			var message = JsonSerializer.Deserialize(e.Event.Data.Span, type, TransactoSerializerOptions.Events)!;
			return Task.WhenAll(_projectors.Where(projection => projection.Checkpoint < e.OriginalPosition)
				.Select(async projector => {
					await using var connection = _connectionFactory();
					await connection.OpenAsync(cancellationToken);
					await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
					var (projection, _) = projector;
					var sqlProjector = new AsyncSqlProjector(projector.Resolver,
						new ConnectedTransactionalSqlCommandExecutor(transaction));
					await sqlProjector.ProjectAsync(message, cancellationToken);
					await projection.WriteCheckpoint(transaction, e.Event.Position, cancellationToken);
					await transaction.CommitAsync(cancellationToken);
				}));
		}
	}
}
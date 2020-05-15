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
using SomeCompany.Framework.Projections;
using SomeCompany.Infrastructure;
using Transacto.Framework;
using Transacto.Infrastructure;

#nullable enable
namespace SomeCompany {
	public class NpgSqlProjectionHost : IHostedService {
		private readonly EventStoreClient _eventStore;
		private readonly IMessageTypeMapper _messageTypeMap;
		private readonly Func<NpgsqlConnection> _connectionFactory;
		private readonly NpgsqlProjectionBase[] _projections;
		private readonly CancellationTokenSource _stopped;

		private int _retryCount;
		private int _subscribed;
		private StreamSubscription? _subscription;
		private CancellationTokenRegistration? _stoppedRegistration;

		public NpgSqlProjectionHost(EventStoreClient eventStore, IMessageTypeMapper messageTypeMap,
			Func<NpgsqlConnection> connectionFactory, params NpgsqlProjectionBase[] projections) {
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

			var projections = await ReadCheckpoints();
			var projector = new CheckpointAwareProjector(_connectionFactory, _messageTypeMap, projections);
			var checkpoint = projections.Select(x => x.checkpoint).Min();

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
				userCredentials: new UserCredentials("admin", "changeit"),
				cancellationToken: _stopped.Token));

			_stoppedRegistration = _stopped.Token.Register(_subscription.Dispose);

			async ValueTask<(NpgsqlProjectionBase projection, Position checkpoint)[]> ReadCheckpoints() {
				await using var connection = _connectionFactory();
				await connection.OpenAsync(cancellationToken);
				return await Task.WhenAll(Array.ConvertAll(_projections,
					async projection => (projection, await projection.ReadCheckpoint(connection, cancellationToken))));
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

		private class CheckpointAwareProjector {
			private readonly Func<NpgsqlConnection> _connectionFactory;
			private readonly IMessageTypeMapper _messageTypeMapper;

			private readonly (NpgsqlProjectionBase projection, Position checkpoint)[] _projections;

			public CheckpointAwareProjector(Func<NpgsqlConnection> connectionFactory,
				IMessageTypeMapper messageTypeMapper,
				(NpgsqlProjectionBase projection, Position checkpoint)[] projections) {
				_connectionFactory = connectionFactory;
				_messageTypeMapper = messageTypeMapper;
				_projections = projections;
			}

			public Task ProjectAsync(StreamSubscription subscription, ResolvedEvent e,
				CancellationToken cancellationToken) {
				var type = _messageTypeMapper.Map(e.Event.EventType);
				if (type == null)
					return Task.CompletedTask;
				var message =
					JsonSerializer.Deserialize(e.Event.Data.Span, type, TransactoSerializerOptions.EventSerializerOptions);
				return Task.WhenAll(_projections.Where(x => x.checkpoint < e.OriginalPosition)
					.Select(x => x.projection)
					.Select(async projection => {
						await using var connection = _connectionFactory();
						await connection.OpenAsync(cancellationToken);
						await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
						var projector = new AsyncSqlProjector(Resolve.WhenEqualToHandlerMessageType(projection),
							new ConnectedTransactionalSqlCommandExecutor(transaction));
						await projector.ProjectAsync(message, cancellationToken);
						await projection.WriteCheckpoint(transaction, e.Event.Position, cancellationToken);
						await transaction.CommitAsync(cancellationToken);
					}));
			}
		}
	}
}

using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EventStore.Client;
using Microsoft.Extensions.Hosting;
using Projac;
using Serilog;
using SqlStreamStore;
using Transacto.Framework;

namespace Transacto.Infrastructure.SqlStreamStore {
	public class StreamStoreProjectionHost : IHostedService {
		private readonly EventStoreClient _eventStore;
		private readonly IMessageTypeMapper _messageTypeMap;
		private readonly IStreamStore _streamStore;
		private readonly StreamStoreProjection[] _projections;
		private readonly CancellationTokenSource _stopped;

		private int _retryCount;
		private int _subscribed;
		private StreamSubscription? _subscription;
		private CancellationTokenRegistration? _stoppedRegistration;

		public StreamStoreProjectionHost(EventStoreClient eventStore, IMessageTypeMapper messageTypeMap,
			IStreamStore streamStore, params StreamStoreProjection[] projections) {
			_eventStore = eventStore;
			_messageTypeMap = messageTypeMap;
			_streamStore = streamStore;
			_projections = projections;
			_stopped = new CancellationTokenSource();

			_retryCount = 0;
			_subscribed = 0;
			_subscription = null;
			_stoppedRegistration = null;
		}

		public Task StartAsync(CancellationToken cancellationToken) =>
			_projections.Length == 0 ? Task.CompletedTask : Subscribe(cancellationToken);

		public Task StopAsync(CancellationToken cancellationToken) {
			_stopped.Cancel();
			_stoppedRegistration?.Dispose();
			return Task.CompletedTask;
		}

		private async Task Subscribe(CancellationToken cancellationToken) {
			if (Interlocked.CompareExchange(ref _subscribed, 1, 0) == 1) {
				return;
			}

			var registration = _stoppedRegistration;
			if (registration != null) {
				await registration.Value.DisposeAsync();
			}

			var projections = await GetProjectors();
			var projector = new CheckpointAwareProjector(_streamStore, _messageTypeMap, projections);
			var checkpoint = projections.Select(x => x.Checkpoint).Min();

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

			Task<CheckpointedProjector[]> GetProjectors() => Task.WhenAll(Array.ConvertAll(_projections,
				async projection => new CheckpointedProjector(
					new Projector<IStreamStore>(Resolve.WhenEqualToHandlerMessageType(projection.Handlers)),
					await projection.ReadCheckpoint(_streamStore, cancellationToken))));
		}

		private record CheckpointedProjector(Projector<IStreamStore> Projector, Position Checkpoint);

		private class CheckpointAwareProjector {
			private readonly IStreamStore _streamStore;
			private readonly IMessageTypeMapper _messageTypeMapper;
			private readonly CheckpointedProjector[] _projectors;

			public CheckpointAwareProjector(IStreamStore streamStore, IMessageTypeMapper messageTypeMapper,
				CheckpointedProjector[] projections) {
				_streamStore = streamStore;
				_messageTypeMapper = messageTypeMapper;
				_projectors = projections;
			}

			public Task ProjectAsync(StreamSubscription subscription, ResolvedEvent e,
				CancellationToken cancellationToken) {
				if (!_messageTypeMapper.TryMap(e.Event.EventType, out var type)) {
					return Task.CompletedTask;
				}

				var message = JsonSerializer.Deserialize(e.Event.Data.Span, type!, TransactoSerializerOptions.Events)!;
				return Task.WhenAll(_projectors.Where(x => x.Checkpoint < e.OriginalPosition)
					.Select(_ => _.Projector.ProjectAsync(_streamStore,
						Envelope.Create(message, e.OriginalEvent.Position), cancellationToken)));
			}
		}
	}
}

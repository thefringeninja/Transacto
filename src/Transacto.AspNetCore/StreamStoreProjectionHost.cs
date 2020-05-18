using System;
using System.Collections.Concurrent;
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
using Transacto.Infrastructure;
using Position = EventStore.Client.Position;
using Resolve = Projac.Resolve;

namespace Transacto {
	public abstract class Envelope {
		private static readonly ConcurrentDictionary<Type, Func<object, Position, Envelope>> Cache =
			new ConcurrentDictionary<Type, Func<object, Position, Envelope>>();

		public object Message { get; }
		public Position Position { get; }

		public static Envelope Create(object message, Position position) =>
			Cache.GetOrAdd(message.GetType(), type => (m, p) => (Envelope)typeof(Envelope<>)
					.MakeGenericType(m.GetType())
					.GetConstructor(new[] {m.GetType(), typeof(Position)})!.Invoke(new[] {m, p}))
				.Invoke(message, position);

		protected Envelope(object message, Position position) {
			Message = message;
			Position = position;
		}
	}

	public class Envelope<T> : Envelope {
		public new T Message { get; }

		public Envelope(T message, Position position) : base(message!, position) {
			Message = message;
		}
	}

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

		public Task StartAsync(CancellationToken cancellationToken) => Subscribe(cancellationToken);

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

			var projections = await ReadCheckpoints();
			var projector = new CheckpointAwareProjector(_streamStore, _messageTypeMap, projections);
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

			Task<(Projection<IStreamStore> projection, Position checkpoint)[]> ReadCheckpoints() =>
				Task.WhenAll(Array.ConvertAll(_projections,
					async projection => ((Projection<IStreamStore>)projection,
						await projection.ReadCheckpoint(_streamStore, cancellationToken))));
		}

		private class CheckpointAwareProjector {
			private readonly IStreamStore _streamStore;
			private readonly IMessageTypeMapper _messageTypeMapper;
			private readonly (Position checkpoint, Projector<IStreamStore> projector)[] _projectors;

			public CheckpointAwareProjector(IStreamStore streamStore,
				IMessageTypeMapper messageTypeMapper,
				(Projection<IStreamStore> projection, Position checkpoint)[] projections) {
				_streamStore = streamStore;
				_messageTypeMapper = messageTypeMapper;
				_projectors = Array.ConvertAll(projections, _ => (_.checkpoint,
					new Projector<IStreamStore>(Resolve.WhenEqualToHandlerMessageType(_.projection.Handlers))));
			}

			public Task ProjectAsync(StreamSubscription subscription, ResolvedEvent e,
				CancellationToken cancellationToken) {
				var type = _messageTypeMapper.Map(e.Event.EventType);
				if (type == null)
					return Task.CompletedTask;
				var message = JsonSerializer.Deserialize(
					e.Event.Data.Span, type, TransactoSerializerOptions.EventSerializerOptions);
				return Task.WhenAll(_projectors.Where(x => x.checkpoint < e.OriginalPosition)
					.Select(_ => _.projector.ProjectAsync(_streamStore,
						Envelope.Create(message, e.OriginalPosition!.Value), cancellationToken)));
			}
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using EventStore.Client;

namespace Transacto.Framework {
	/// <summary>
	/// Tracks changes of attached aggregates.
	/// </summary>
	public class UnitOfWork {
		private readonly EventStoreClient _eventStore;
		private readonly IMessageTypeMapper _messageTypeMapper;
		private readonly JsonSerializerOptions _serializerOptions;
		private static readonly AsyncLocal<UnitOfWork?> Storage = new AsyncLocal<UnitOfWork?>();

		private readonly IDictionary<string, (AggregateRoot aggregate, Optional<long> expectedVersion)> _aggregates;

		/// <summary>
		/// Starts a <see cref="UnitOfWork"/>.
		/// </summary>
		/// <returns>An <see cref="IDisposable"/> that ends the <see cref="UnitOfWork"/> when invoked.</returns>
		public static IDisposable Start(EventStoreClient eventStore, IMessageTypeMapper messageTypeMapper,
			JsonSerializerOptions serializerOptions) {
			Storage.Value = new UnitOfWork(eventStore, messageTypeMapper, serializerOptions);

			return new DisposableAction(() => Storage.Value = null);
		}

		/// <summary>
		/// The current <see cref="UnitOfWork"/>.
		/// </summary>
		public static UnitOfWork Current => Storage.Value ?? throw new UnitOfWorkNotStartedException();

		/// <summary>
		/// Initializes a new instance of the <see cref="UnitOfWork"/> class.
		/// </summary>
		private UnitOfWork(EventStoreClient eventStore, IMessageTypeMapper messageTypeMapper, JsonSerializerOptions serializerOptions) {
			_eventStore = eventStore;
			_messageTypeMapper = messageTypeMapper;
			_serializerOptions = serializerOptions;
			_aggregates = new Dictionary<string, (AggregateRoot, Optional<long>)>();
		}

		/// <summary>
		/// Attaches the specified aggregate.
		/// </summary>
		/// <param name="streamName">The identifier.</param>
		/// <param name="aggregate">The aggregate.</param>
		/// <param name="expectedVersion">The expected version.</param>
		/// <exception cref="System.ArgumentNullException">Thrown when the <paramref name="aggregate"/> is null.</exception>
		public void Attach(string streamName, AggregateRoot aggregate, Optional<long> expectedVersion = default) {
			if (_aggregates.ContainsKey(streamName))
				throw new ArgumentException();
			_aggregates.Add(streamName, (aggregate, expectedVersion));
		}

		/// <summary>
		/// Attempts to get the <see cref="AggregateRoot"/> using the specified aggregate identifier.
		/// </summary>
		/// <param name="streamName">The aggregate identifier.</param>
		/// <param name="aggregate">The aggregate if found, otherwise <c>null</c>.</param>
		/// <returns><c>true</c> if the aggregate was found, otherwise <c>false</c>.</returns>
		public bool TryGet(string streamName, out AggregateRoot? aggregate) {
			aggregate = null;
			if (!_aggregates.TryGetValue(streamName, out var x)) {
				return false;
			}

			aggregate = x.aggregate;
			return true;
		}

		/// <summary>
		/// Determines whether this instance has aggregates with state changes.
		/// </summary>
		/// <returns>
		///   <c>true</c> if this instance has aggregates with state changes; otherwise, <c>false</c>.
		/// </returns>
		public bool HasChanges => _aggregates.Values.Any(_ => _.aggregate.HasChanges);

		/// <summary>
		/// Gets the aggregates with state changes.
		/// </summary>
		/// <returns>An enumeration of <see cref="AggregateRoot"/>.</returns>
		public IEnumerable<(string streamName, AggregateRoot aggregate, Optional<long> expectedVersion)> GetChanges() =>
			_aggregates.Where(_ => _.Value.aggregate.HasChanges)
				.Select(_ => (_.Key, _.Value.aggregate, _.Value.expectedVersion));

		private class DisposableAction : IDisposable {
			private readonly Action _onDispose;

			public DisposableAction(Action onDispose) {
				_onDispose = onDispose;
			}

			public void Dispose() => _onDispose();
		}
	}
}

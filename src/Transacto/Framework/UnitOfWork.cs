using System;
using System.Collections.Generic;
using System.Linq;

namespace Transacto.Framework {
    /// <summary>
    /// Tracks changes of attached aggregates.
    /// </summary>
    public class UnitOfWork {
        private readonly IDictionary<string, (AggregateRoot aggregate, Optional<ulong> expectedVersion)> _aggregates;

        /// <summary>
        /// Initializes a new instance of the <see cref="UnitOfWork"/> class.
        /// </summary>
        public UnitOfWork() {
            _aggregates = new Dictionary<string, (AggregateRoot, Optional<ulong>)>();
        }

        /// <summary>
        /// Attaches the specified aggregate.
        /// </summary>
        /// <param name="streamName">The identifier.</param>
        /// <param name="aggregate">The aggregate.</param>
        /// <param name="expectedVersion">The expected version.</param>
        /// <exception cref="System.ArgumentNullException">Thrown when the <paramref name="aggregate"/> is null.</exception>
        public void Attach(string streamName, AggregateRoot aggregate, Optional<ulong> expectedVersion = default) {
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
        public IEnumerable<(string streamName, AggregateRoot aggregate, Optional<ulong> expectedVersion)> GetChanges() =>
            _aggregates.Where(_ => _.Value.aggregate.HasChanges).Select(_ => (_.Key, _.Value.aggregate, _.Value.expectedVersion));
    }
}

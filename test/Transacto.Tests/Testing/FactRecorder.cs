using System.Collections.Generic;
using System.Linq;
using Transacto.Framework;

namespace Transacto.Testing {
    internal class FactRecorder : IFactRecorder {
        private readonly List<Fact> _recordedFacts;
        private readonly IDictionary<string, AggregateRoot> _aggregates;

        public FactRecorder() {
            _recordedFacts = new List<Fact>();
            _aggregates = new Dictionary<string, AggregateRoot>();
        }

        public void Record(string identifier, IEnumerable<object> events) =>
            Record(events.Select(e => new Fact(identifier, e)));

        public void Record(IEnumerable<Fact> facts) => _recordedFacts.AddRange(facts);
        public void Record(string identifier, AggregateRoot aggregate) => _aggregates[identifier] = aggregate;

        public IAsyncEnumerable<Fact> GetFacts() =>
            _recordedFacts.ToAsyncEnumerable()
                .Concat(_aggregates.SelectMany(x => x.Value.GetChanges().Select(e => new Fact(x.Key, e)))
	                .ToAsyncEnumerable());
    }
}

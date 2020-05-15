using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Transacto.Framework {
    public abstract class AggregateRoot {
        private readonly IDictionary<Type, Action<object>> _router;
        private readonly IList<object> _changes;

        protected AggregateRoot() {
            _changes = new List<object>();
            _router = new Dictionary<Type, Action<object>>();
        }

        public async ValueTask<Optional<long>> LoadFromHistory(IAsyncEnumerable<object> events) {
            var i = -1;

            await foreach (var e in events) {
                Apply(e, true);
                i++;
            }

            return i == -1 ? Optional<long>.Empty : i;
        }

        public void MarkChangesAsCommitted() => _changes.Clear();
        public IEnumerable<object> GetChanges() => _changes.AsEnumerable();
        protected void Register<T>(Action<T> apply) => _router.Add(typeof(T), e => apply((T)e));
        public bool HasChanges => _changes.Count > 0;

        protected void Apply(object e, bool historical = false) {
            if (_router.TryGetValue(e.GetType(), out var handle)) {
                handle(e);
            }

            if (!historical) {
	            _changes.Add(e);
            }
        }
    }
}

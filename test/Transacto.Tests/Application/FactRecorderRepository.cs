using Transacto.Framework;
using Transacto.Testing;

namespace Transacto.Application;

internal class FactRecorderRepository<T>(IFactRecorder facts)
	where T : IAggregateRoot<T> {
	public ValueTask<Optional<T>> GetOptional(string identifier,
		CancellationToken cancellationToken = default) {
		var facts1 = facts.GetFacts().Where(x => x.Identifier == identifier)
			.ToArray();

		if (facts1.Length == 0) {
			return new(Optional<T>.Empty);
		}

		var aggregateRoot = T.Factory();
		foreach (var fact in facts1) {
			aggregateRoot.ReadFromHistory(fact.Event);
		}

		facts.Attach(aggregateRoot.Id, aggregateRoot);
		return new(aggregateRoot);
	}

	public void Add(T aggregateRoot) => facts.Record(aggregateRoot.Id, aggregateRoot.GetChanges());
}

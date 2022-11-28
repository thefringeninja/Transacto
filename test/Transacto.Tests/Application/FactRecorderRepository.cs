using Transacto.Framework;
using Transacto.Testing;

namespace Transacto.Application;

internal class FactRecorderRepository<T> where T : IAggregateRoot<T> {
	private readonly IFactRecorder _facts;

	public FactRecorderRepository(IFactRecorder facts) {
		_facts = facts;
	}

	public ValueTask<Optional<T>> GetOptional(string identifier,
		CancellationToken cancellationToken = default) {
		var facts = _facts.GetFacts().Where(x => x.Identifier == identifier)
			.ToArray();

		if (facts.Length == 0) {
			return new(Optional<T>.Empty);
		}

		var aggregateRoot = T.Factory();
		foreach (var fact in facts) {
			aggregateRoot.ReadFromHistory(fact.Event);
		}

		_facts.Attach(aggregateRoot.Id, aggregateRoot);
		return new(aggregateRoot);
	}

	public void Add(T aggregateRoot) => _facts.Record(aggregateRoot.Id, aggregateRoot.GetChanges());
}

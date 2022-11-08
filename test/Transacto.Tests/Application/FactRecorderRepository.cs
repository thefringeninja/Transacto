using Transacto.Framework;
using Transacto.Testing;

namespace Transacto.Application; 

internal class FactRecorderRepository<T> where T : AggregateRoot {
	private readonly IFactRecorder _facts;
	private readonly Func<T> _factory;

	public FactRecorderRepository(IFactRecorder facts, Func<T> factory) {
		_facts = facts;
		_factory = factory;
	}

	public ValueTask<Optional<T>> GetOptional(string identifier,
		CancellationToken cancellationToken = default) {
		var facts = _facts.GetFacts().Where(x => x.Identifier == identifier)
			.ToArray();

		if (facts.Length == 0) {
			return new(Optional<T>.Empty);
		}

		var aggregateRoot = _factory();
		aggregateRoot.LoadFromHistory(facts.Select(x => x.Event));
		_facts.Attach(aggregateRoot.Id, aggregateRoot);
		return new(aggregateRoot);
	}

	public void Add(T aggregateRoot) => _facts.Record(aggregateRoot.Id, aggregateRoot.GetChanges());
}

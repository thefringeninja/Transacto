using Transacto.Framework;
using Transacto.Testing;

namespace Transacto.Application;

internal class FactRecorderRepository<T>(IFactRecorder factRecorder)
	where T : IAggregateRoot<T> {
	public ValueTask<Optional<T>> GetOptional(string identifier, CancellationToken cancellationToken = default) =>
		factRecorder.GetFacts().Where(x => x.Identifier == identifier)
			.ToArray() switch {
			{ Length: 0 } => new(Optional<T>.Empty),
			_ => Load(factRecorder.GetFacts().Where(x => x.Identifier == identifier)
				.ToArray())
		};

	private ValueTask<Optional<T>> Load(Fact[] facts) {
		var aggregateRoot = T.Factory();
		foreach (var fact in facts) {
			aggregateRoot.ReadFromHistory(fact.Event);
		}

		factRecorder.Attach(aggregateRoot.Id, aggregateRoot);
		return new(aggregateRoot);
	}

	public void Add(T aggregateRoot) => factRecorder.Record(aggregateRoot.Id, aggregateRoot.GetChanges());
}

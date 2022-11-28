using Transacto.Framework;

namespace Transacto.Testing;

internal class FactRecorder : IFactRecorder {
	private readonly IDictionary<string, IAggregateRoot> _aggregates;
	private readonly List<Fact> _recordedFacts;

	public FactRecorder() {
		_recordedFacts = new List<Fact>();
		_aggregates = new Dictionary<string, IAggregateRoot>();
	}

	public void Record(string identifier, IEnumerable<object> events) =>
		Record(events.Select(e => new Fact(identifier, e)));

	public void Record(IEnumerable<Fact> facts) => _recordedFacts.AddRange(facts);

	public void Attach(string identifier, IAggregateRoot aggregate) => _aggregates[identifier] = aggregate;

	public IEnumerable<Fact> GetFacts() =>
		_recordedFacts.Concat(_aggregates.SelectMany(x => x.Value.GetChanges().Select(e => new Fact(x.Key, e))));
}

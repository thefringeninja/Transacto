using Transacto.Framework;

namespace Transacto.Testing;

public class FactRecorder : IFactRecorder {
	private readonly IDictionary<string, IAggregateRoot> _aggregates = new Dictionary<string, IAggregateRoot>();
	private readonly List<Fact> _recordedFacts = [];

	public void Record(string identifier, IEnumerable<object> events) =>
		Record(events.Select(e => new Fact(identifier, e)));

	public void Record(IEnumerable<Fact> facts) => _recordedFacts.AddRange(facts);

	public void Attach(string identifier, IAggregateRoot aggregate) => _aggregates[identifier] = aggregate;

	public IEnumerable<Fact> GetFacts() =>
		_recordedFacts.Concat(_aggregates.SelectMany(x => x.Value.GetChanges().Select(e => new Fact(x.Key, e))));
}

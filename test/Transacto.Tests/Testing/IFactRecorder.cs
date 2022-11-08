using Transacto.Framework;

namespace Transacto.Testing;

public interface IFactRecorder {
	void Record(string identifier, IEnumerable<object> events);
	void Record(IEnumerable<Fact> facts);
	void Attach(string identifier, AggregateRoot aggregate);
	IEnumerable<Fact> GetFacts();
}

namespace Transacto.Framework; 

public abstract class AggregateRoot {
	private readonly IList<object> _changes;

	public bool HasChanges => _changes.Count > 0;
	public abstract string Id { get; }

	protected AggregateRoot() {
		_changes = new List<object>();
	}

	public Optional<long> LoadFromHistory(IEnumerable<object> events) {
		var i = -1;

		foreach (var e in events) {
			Apply(e, true);
			i++;
		}

		return i == -1 ? Optional<long>.Empty : i;
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

	protected abstract void ApplyEvent(object e);

	protected void Apply(object e, bool historical = false) {
		ApplyEvent(e);

		if (!historical) {
			_changes.Add(e);
		}
	}
}

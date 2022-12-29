namespace Transacto.Framework;

public abstract class AggregateRoot : IAggregateRoot {
	private readonly IList<object> _changes;

	public bool HasChanges => _changes.Count > 0;
	public abstract string Id { get; }

	protected AggregateRoot() => _changes = new List<object>();

	public void ReadFromHistory(object @event) => Apply(@event, true);
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

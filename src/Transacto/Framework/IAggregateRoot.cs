namespace Transacto.Framework;

public interface IAggregateRoot<out T> : IAggregateRoot where T : IAggregateRoot<T> {
	static abstract T Factory();
}

public interface IAggregateRoot {
	bool HasChanges { get; }
	string Id { get; }
	void ReadFromHistory(object @event);
	void MarkChangesAsCommitted();
	IEnumerable<object> GetChanges();
}

namespace Transacto.Framework.Projections;

public class InMemoryProjectionDatabase(IEnumerable<MemoryReadModel> readModels) {
	private readonly Dictionary<Type, MemoryReadModel> _readModelsByType =
		new(readModels.Select(x => new KeyValuePair<Type, MemoryReadModel>(x.GetType(), x)));

	public Optional<T> Get<T>() where T : MemoryReadModel =>
		_readModelsByType.TryGetValue(typeof(T), out var value) && value is T readModel
			? new Optional<T>(readModel)
			: Optional<T>.Empty;

	public void Set<T>(T instance) where T : MemoryReadModel => _readModelsByType[typeof(T)] =  instance;

	public void Clear() => _readModelsByType.Clear();
}

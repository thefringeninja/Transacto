using System.Collections.Concurrent;

namespace Transacto.Framework.Projections;

public class InMemoryProjectionDatabase(IEnumerable<MemoryReadModel> readModels) {
	private readonly ConcurrentDictionary<Type, MemoryReadModel> _readModelsByType =
		new(readModels.Select(x => new KeyValuePair<Type, MemoryReadModel>(x.GetType(), x)));
	public Option<T> Get<T>() where T : MemoryReadModel =>
		_readModelsByType.TryGetValue(typeof(T), out var value) && value is T readModel
			? (Some<T>)readModel
			: new None();

	public void Set<T>(T instance) where T : MemoryReadModel =>
		_readModelsByType.AddOrUpdate(typeof(T), static (_, instance) => instance, static (_, _, instance) => instance,
			instance);

	public void Clear() => _readModelsByType.Clear();
}

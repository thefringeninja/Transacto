using System;
using System.Collections.Generic;
using System.Linq;

namespace Transacto.Framework.Projections {
	public class InMemorySession {
		private readonly IDictionary<Type, IMemoryReadModel> _readModelsByType;

		public InMemorySession(IEnumerable<IMemoryReadModel> readModels) {
			_readModelsByType = new Dictionary<Type, IMemoryReadModel>(
				readModels.Select(x => new KeyValuePair<Type, IMemoryReadModel>(x.GetType(), x)));
		}

		public Optional<T> Get<T>() where T : class, IMemoryReadModel =>
			_readModelsByType.TryGetValue(typeof(T), out var value) && value is T readModel
				? new Optional<T>(readModel)
				: Optional<T>.Empty;
	}
}

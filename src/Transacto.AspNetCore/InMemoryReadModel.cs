using System;
using System.Collections;
using System.Collections.Concurrent;

namespace Transaction.AspNetCore {
	public class InMemoryReadModel {
		private readonly ConcurrentDictionary<string, object> _readModels;

		public InMemoryReadModel() {
			_readModels = new ConcurrentDictionary<string, object>();
		}

		public void Update<T>(string key, Action<T> action, Func<T> factory = null) {
			factory ??= Activator.CreateInstance<T>;
			var maybeTarget = _readModels.GetOrAdd(key, _ => factory());

			if (!(maybeTarget is T target)) {
				return;
			}

			if (!(target is IEnumerable)) {
				action(target);
				return;
			}
			lock (target) {
				action(target);
			}
		}

		public bool TryGet<T>(string key, Func<T, T> clone, out T target) => TryGet<T, T>(key, clone, out target);

		public bool TryGet<T, TTransformed>(string key, Func<T, TTransformed> transform, out TTransformed target) {
			if (!_readModels.TryGetValue(key, out var maybeTarget) || !(maybeTarget is T value)) {
				target = default;
				return false;
			}

			if (value is IEnumerable) {
				lock (value) {
					target = transform(value);
				}
			} else {
				target = transform(value);
			}

			return true;
		}
	}
}

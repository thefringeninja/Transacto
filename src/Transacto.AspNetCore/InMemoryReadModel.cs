using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Transacto {
	public class InMemoryReadModel {
		private readonly ConcurrentDictionary<string, IInMemoryReadModelEntry> _readModels;

		public InMemoryReadModel() {
			_readModels = new ConcurrentDictionary<string, IInMemoryReadModelEntry>();
		}

		public bool TryGetValue<T>(string key, out InMemoryReadModelEntry<T>? value) where T : class {
			if (!_readModels.TryGetValue(key, out var maybeEntry) ||
			    !(maybeEntry is InMemoryReadModelEntry<T> entry)) {
				value = default;
				return false;
			}

			value = entry;
			return true;
		}

		public bool TryRemove<T>(string key, out T? value) where T: class {
			if (!_readModels.TryGetValue(key, out var maybeEntry) || !(maybeEntry is InMemoryReadModelEntry<T> entry)) {
				value = default;
				return false;
			}

			_readModels.TryRemove(key, out _);
			value = entry.Item;
			return true;
		}

		public void AddOrUpdate<T>(string key, Func<T> factory, Action<T> update) where T : class {
			_readModels.AddOrUpdate(key, _ => {
				var entry = factory();
				update(entry);
				return new InMemoryReadModelEntry<T>(entry);
			}, (_, maybeEntry) => {
				if (!(maybeEntry is InMemoryReadModelEntry<T> entry)) throw new InvalidOperationException();
				using (maybeEntry.Write()) {
					update(entry.Item);
				}

				return maybeEntry;
			});
		}
	}

	public interface IInMemoryReadModelEntry : IDisposable {
			object Item { get; }
			IDisposable Read();
			IDisposable Write();
		}

		public class InMemoryReadModelEntry<T> : IInMemoryReadModelEntry where T : class {
			private readonly ReaderWriterLockSlim _locker;
			object IInMemoryReadModelEntry.Item => Item;
			public T Item { get; }

			public InMemoryReadModelEntry(T item) {
				Item = item;
				_locker = new ReaderWriterLockSlim();
			}

			public IDisposable Read() => new ReadLockToken(_locker);

			public IDisposable Write() => new WriteLockToken(_locker);

			public void Dispose() => _locker.Dispose();

			private class ReadLockToken : IDisposable {
				private readonly ReaderWriterLockSlim _locker;

				public ReadLockToken(ReaderWriterLockSlim locker) {
					_locker = locker;
					_locker.EnterReadLock();
				}

				public void Dispose() => _locker.ExitReadLock();
			}

			private class WriteLockToken : IDisposable {
				private readonly ReaderWriterLockSlim _locker;

				public WriteLockToken(ReaderWriterLockSlim locker) {
					_locker = locker;
					_locker.EnterWriteLock();
				}

				public void Dispose() => _locker.ExitWriteLock();
			}
		}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Transacto.Domain;
using Transacto.Messages;

namespace Transacto.Framework {
	public class MessageTypeMapper : IMessageTypeMapper {
		private readonly IDictionary<string, Type> _storageTypeToType;
		private readonly IDictionary<Type, string> _typeToStorageType;

		public IEnumerable<string> StorageTypes => _storageTypeToType.Keys;
		public IEnumerable<Type> Types => _typeToStorageType.Keys;

		public static IMessageTypeMapper ScopedFromType(Type type) =>
			new ReflectionMessageTypeMapper(type.Assembly, type.Namespace);

		public static IMessageTypeMapper Create(params IMessageTypeMapper[] messageTypeMappers)
			=> new CompositeMessageTypeMapper(messageTypeMappers.Concat(new[] {TransactoMessageTypeMapper.Instance})
				.ToArray());

		public MessageTypeMapper(IEnumerable<Type> types) {
			_typeToStorageType = types.ToDictionary(type => type, type => type.Name);
			_storageTypeToType = _typeToStorageType.ToDictionary(pair => pair.Value, pair => pair.Key);
		}

		public bool TryMap(string storageType, out Type type) => _storageTypeToType.TryGetValue(storageType, out type!);
		public bool TryMap(Type type, out string storageType) => _typeToStorageType.TryGetValue(type, out storageType!);

		private class TransactoMessageTypeMapper : IMessageTypeMapper {
			public static readonly IMessageTypeMapper Instance = new TransactoMessageTypeMapper();

			private readonly IMessageTypeMapper _inner;

			public bool TryMap(string storageType, out Type type) => _inner.TryMap(storageType, out type);
			public bool TryMap(Type type, out string storageType) => _inner.TryMap(type, out storageType);

			public IEnumerable<string> StorageTypes => _inner.StorageTypes;
			public IEnumerable<Type> Types => _inner.Types;

			private TransactoMessageTypeMapper() => _inner = new CompositeMessageTypeMapper(
				ScopedFromType(typeof(AccountDefined)), new MessageTypeMapper(new[] { typeof(JournalEntry) }));
		}

		private class CompositeMessageTypeMapper : IMessageTypeMapper {
			private readonly IEnumerable<IMessageTypeMapper> _messageTypeMappers;

			public IEnumerable<string> StorageTypes => _messageTypeMappers.SelectMany(m => m.StorageTypes);
			public IEnumerable<Type> Types => _messageTypeMappers.SelectMany(m => m.Types);

			public CompositeMessageTypeMapper(params IMessageTypeMapper[] messageTypeMappers) {
				var duplicates = (from m in messageTypeMappers
					from s in m.StorageTypes
					group s by s
					into g
					where g.Count() > 1
					select g.Key).ToArray();
				if (duplicates.Length > 0) {
					throw new ArgumentException("Duplicate types registered.", nameof(messageTypeMappers));
				}

				_messageTypeMappers = messageTypeMappers;
			}

			public bool TryMap(string storageType, out Type type) {
				type = default!;
				foreach (var messageTypeMapper in _messageTypeMappers) {
					if (messageTypeMapper.TryMap(storageType, out type)) {
						return true;
					}
				}

				return false;
			}

			public bool TryMap(Type type, out string storageType) {
				storageType = default!;
				foreach (var messageTypeMapper in _messageTypeMappers) {
					if (messageTypeMapper.TryMap(type, out storageType)) {
						return true;
					}
				}

				return false;
			}
		}

		private class ReflectionMessageTypeMapper : IMessageTypeMapper {
			private readonly IMessageTypeMapper _inner;

			public ReflectionMessageTypeMapper(Assembly messageAssembly, string? messageNamespace) {
				_inner = new MessageTypeMapper(messageAssembly.DefinedTypes.Where(IsMessageType(messageNamespace)));
			}

			private static Func<Type, bool> IsMessageType(string? messageNamespace) => type =>
				string.Equals(messageNamespace, type.Namespace);

			public bool TryMap(string storageType, out Type type) => _inner.TryMap(storageType, out type);
			public bool TryMap(Type type, out string storageType) => _inner.TryMap(type, out storageType);
			public IEnumerable<string> StorageTypes => _inner.StorageTypes;
			public IEnumerable<Type> Types => _inner.Types;
		}
	}
}

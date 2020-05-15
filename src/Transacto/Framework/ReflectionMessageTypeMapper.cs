using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Transacto.Messages;

namespace Transacto.Framework {
	internal class TransactoMessageTypeMapper : IMessageTypeMapper {
		public static IMessageTypeMapper Instance = new TransactoMessageTypeMapper();

		private readonly IMessageTypeMapper _inner;

		public IEnumerable<string> StorageTypes => _inner.StorageTypes;
		public IEnumerable<Type> Types => _inner.Types;

		private TransactoMessageTypeMapper() => _inner = MessageTypeMapper.ScopedFromType(typeof(AccountDefined));

		public string? Map(Type type) => _inner.Map(type);

		public Type? Map(string storageType) => _inner.Map(storageType);
	}

	internal class CompositeMessageTypeMapper : IMessageTypeMapper {
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
				throw new ArgumentException();
			}

			_messageTypeMappers = messageTypeMappers;
		}

		public string? Map(Type type) => _messageTypeMappers.Select(x => x.Map(type)).FirstOrDefault(t => t != null);

		public Type? Map(string storageType) =>
			_messageTypeMappers.Select(x => x.Map(storageType)).FirstOrDefault(t => t != null);
	}

	internal class ReflectionMessageTypeMapper : IMessageTypeMapper {
		private readonly IMessageTypeMapper _inner;

		public ReflectionMessageTypeMapper(Assembly messageAssembly, string messageNamespace) {
			_inner = new MessageTypeMapper(messageAssembly.DefinedTypes.Where(IsMessageType(messageNamespace)));
		}

		private static Func<Type, bool> IsMessageType(string messageNamespace) =>
			type => type.Namespace?.Equals(messageNamespace) ?? false;

		public string? Map(Type type) => _inner.Map(type);

		public Type? Map(string storageType) => _inner.Map(storageType);

		public IEnumerable<string> StorageTypes => _inner.StorageTypes;

		public IEnumerable<Type> Types => _inner.Types;
	}

	public class MessageTypeMapper : IMessageTypeMapper {
		private readonly IDictionary<string, Type> _storageTypeToType;
		private readonly IDictionary<Type, string> _typeToStorageType;

		public IEnumerable<string> StorageTypes => _storageTypeToType.Keys;
		public IEnumerable<Type> Types => _typeToStorageType.Keys;

		public static IMessageTypeMapper ScopedFromType(Type type) {
			if (type.Namespace == null) {
				throw new ArgumentNullException(nameof(type.Namespace));
			}

			return new ReflectionMessageTypeMapper(type.Assembly, type.Namespace);
		}

		public static IMessageTypeMapper Create(params MessageTypeMapper[] messageTypeMappers)
			=> new CompositeMessageTypeMapper(messageTypeMappers.Concat(new[] {TransactoMessageTypeMapper.Instance})
				.ToArray());

		public MessageTypeMapper(IEnumerable<Type> types) {
			_typeToStorageType = types.ToDictionary(type => type, type => type.Name);
			_storageTypeToType = _typeToStorageType.ToDictionary(pair => pair.Value, pair => pair.Key);
		}

		public string? Map(Type type) => _typeToStorageType.TryGetValue(type, out var storageType) ? storageType : null;

		public Type? Map(string storageType) => _storageTypeToType.TryGetValue(storageType, out var type) ? type : null;
	}
}

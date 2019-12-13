using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Transacto.Messages;

namespace Transacto.Framework {
	internal class TransactoMessageTypeMapper : IMessageTypeMapper {
		public static TransactoMessageTypeMapper Instance = new TransactoMessageTypeMapper();

		private readonly IMessageTypeMapper _inner;

		public TransactoMessageTypeMapper() => _inner =
			new ReflectionMessageTypeMapper(typeof(AccountDefined).Assembly, typeof(AccountDefined).Namespace!);

		public string? Map(Type type) => _inner.Map(type);

		public Type? Map(string storageType) => _inner.Map(storageType);
	}

	public class CompositeMessageTypeMapper : IMessageTypeMapper {
		private readonly IEnumerable<IMessageTypeMapper> _messageTypeMappers;
		public CompositeMessageTypeMapper(params IMessageTypeMapper[] messageTypeMappers) {
			_messageTypeMappers = messageTypeMappers.Concat(new[] {TransactoMessageTypeMapper.Instance});
		}

		public string? Map(Type type) => _messageTypeMappers.Select(x => x.Map(type)).FirstOrDefault(t => t != null);

		public Type? Map(string storageType) =>
			_messageTypeMappers.Select(x => x.Map(storageType)).FirstOrDefault(t => t != null);
	}

	public class ReflectionMessageTypeMapper : IMessageTypeMapper {
		private readonly IDictionary<string, Type> _storageTypeToType;
		private readonly IDictionary<Type, string> _typeToStorageType;

		public ReflectionMessageTypeMapper(Assembly messageAssembly, string messageNamespace) : this(
			messageAssembly.DefinedTypes.Where(IsMessageType(messageNamespace))) {
		}

		public ReflectionMessageTypeMapper(IEnumerable<Type> types) {
			_typeToStorageType = types.ToDictionary(type => type, type => type.Name);
			_storageTypeToType = _typeToStorageType.ToDictionary(pair => pair.Value, pair => pair.Key);
		}

		public string? Map(Type type) => _typeToStorageType.TryGetValue(type, out var storageType) ? storageType : null;

		public Type? Map(string storageType) => _storageTypeToType.TryGetValue(storageType, out var type) ? type : null;

		private static Func<Type, bool> IsMessageType(string messageNamespace) =>
			type => type.Namespace?.Equals(messageNamespace) ?? false;
	}
}

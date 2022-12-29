using Transacto.Domain;
using Transacto.Framework.Messages;
using Transacto.Messages;

namespace Transacto.Framework;

public class MessageTypeMapperTests {
	private readonly IMessageTypeMapper _sut;

	public MessageTypeMapperTests() {
		_sut = MessageTypeMapper.Create(MessageTypeMapper.ScopedFromType(typeof(SomeEvent)));
	}

	public void DuplicateTypeRegistrationThrows() {
		Assert.Throws<ArgumentException>(() => MessageTypeMapper.Create(
			MessageTypeMapper.ScopedFromType(typeof(SomeEvent)),
			MessageTypeMapper.ScopedFromType(typeof(SomeEvent))));
	}

	public void TryMapTypeReturnsExpectedResult() {
		Assert.True(_sut.TryMap(typeof(SomeEvent), out var storageType));
		Assert.Equal(nameof(SomeEvent), storageType);
	}

	public void TryMapStorageTypeReturnsExpectedResult() {
		Assert.True(_sut.TryMap(nameof(SomeEvent), out var type));
		Assert.Equal(typeof(SomeEvent), type);
	}

	public void TryMapNonExistingTypeReturnsFalse() {
		Assert.False(_sut.TryMap(typeof(MessageTypeMapperTests), out var storageType));
		Assert.Equal(default, storageType);
	}

	public void TryMapNonExistingStringReturnsFalse() {
		Assert.False(_sut.TryMap(nameof(MessageTypeMapperTests), out var type));
		Assert.Equal(default, type);
	}

	public void MapTypeReturnsExpectedResult() {
		Assert.Equal(nameof(SomeEvent), _sut.Map(typeof(SomeEvent)));
	}

	public void MapStorageTypeReturnsExpectedResult() {
		Assert.Equal(typeof(SomeEvent), _sut.Map(nameof(SomeEvent)));
	}

	public void MapNonExistingTypeThrows() {
		var ex = Assert.Throws<TypeNotFoundException>(() => _sut.Map(typeof(MessageTypeMapperTests)));
		Assert.Equal(typeof(MessageTypeMapperTests), ex.Type);
	}

	public void MapNonExistingStorageTypeThrows() {
		var ex = Assert.Throws<StorageTypeNotFoundException>(() => _sut.Map(nameof(MessageTypeMapperTests)));
		Assert.Equal(nameof(MessageTypeMapperTests), ex.StorageType);
	}

	public void DefaultConfigurationIncludesTransactoMessages() {
		var sut = MessageTypeMapper.Create();

		var messageTypes = typeof(AccountDefined).Assembly.GetTypes()
			.Where(type => string.Equals(type.Namespace, typeof(AccountDefined).Namespace))
			.Concat(new[] { typeof(JournalEntry) });

		Assert.Equal(messageTypes, sut.Types);
		Assert.Equal(messageTypes.Select(x => x.Name), sut.StorageTypes);
	}
}

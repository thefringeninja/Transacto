using System.Text.Json;
using Transacto.Domain;

namespace Transacto.Infrastructure;

public class CommandSerializerOptionTests {
	public void Deserialization() {
		var property = Guid.NewGuid().ToString();
		var anotherProperty = Guid.NewGuid().ToString();
		var transactionId = Guid.NewGuid();
		var json = $@"{{
	""property"": ""{property}"",
	""businessTransaction"": {{
		""testBusinessTransaction"": {JsonSerializer.Serialize(new TestBusinessTransaction { TransactionId = transactionId })}
	}},
    ""anotherProperty"": ""{anotherProperty}""
}}";

		var command = JsonSerializer.Deserialize<TestCommand>(json,
			TransactoSerializerOptions.BusinessTransactions(typeof(TestBusinessTransaction)))!;

		Assert.Equal(property, command.Property);
		Assert.Equal(anotherProperty, command.AnotherProperty);

		var transaction = Assert.IsType<TestBusinessTransaction>(command.BusinessTransaction);
		Assert.Equal(transactionId, transaction.TransactionId);
	}

	public void DeserializationWhenBusinessTransactionTypeDoesNotExist() {
		var property = Guid.NewGuid().ToString();
		var anotherProperty = Guid.NewGuid().ToString();
		var transactionId = Guid.NewGuid();
		var json = $@"{{
	""property"": ""{property}"",
	""businessTransaction"": {{
		""doesNotExist"": {JsonSerializer.Serialize(new TestBusinessTransaction { TransactionId = transactionId })}
	}},
    ""anotherProperty"": ""{anotherProperty}""
}}";

		var command = JsonSerializer.Deserialize<TestCommand>(json,
			TransactoSerializerOptions.BusinessTransactions(typeof(TestBusinessTransaction)))!;

		Assert.Equal(property, command.Property);
		Assert.Equal(anotherProperty, command.AnotherProperty);

		Assert.Null(command.BusinessTransaction);
	}

	public void Serialization() {
		var sut = TransactoSerializerOptions.BusinessTransactions(typeof(TestBusinessTransaction));

		var testBusinessTransaction = new TestBusinessTransaction {
			TransactionId = Guid.NewGuid()
		};
		var dto = new TestCommand {
			Property = Guid.NewGuid().ToString(),
			AnotherProperty = Guid.NewGuid().ToString(),
			BusinessTransaction = testBusinessTransaction
		};

		var copy = JsonSerializer.Deserialize<TestCommand>(JsonSerializer.Serialize(dto, sut), sut)!;

		Assert.Equal(dto.Property, copy.Property);
		Assert.Equal(dto.AnotherProperty, copy.AnotherProperty);
		var transaction = Assert.IsType<TestBusinessTransaction>(dto.BusinessTransaction);
		Assert.Equal(testBusinessTransaction.TransactionId, transaction.TransactionId);
	}

	private class TestCommand {
		public string AnotherProperty { get; set; } = null!;
		public string Property { get; set; } = null!;
		public IBusinessTransaction BusinessTransaction { get; set; } = null!;
	}

	internal class TestBusinessTransaction : IBusinessTransaction {
		public Guid TransactionId { get; set; }

		public GeneralLedgerEntrySequenceNumber SequenceNumber { get; }
		public IEnumerable<object> GetTransactionItems() => Enumerable.Empty<object>();
	}
}

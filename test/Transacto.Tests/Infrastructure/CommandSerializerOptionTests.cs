using System;
using System.Collections.Generic;
using System.Text.Json;
using Transacto.Domain;
using Transacto.Framework;
using Xunit;

namespace Transacto.Infrastructure {
	public class CommandSerializerOptionTests {
		[Fact]
		public void Deserialization() {
			var property = Guid.NewGuid().ToString();
			var anotherProperty = Guid.NewGuid().ToString();
			var transactionId = Guid.NewGuid();
			var json = $@"{{
	""property"": ""{property}"",
	""businessTransaction"": {{
		""testBusinessTransaction"": {JsonSerializer.Serialize(new TestBusinessTransaction {TransactionId = transactionId, Version = 1})}
	}},
    ""anotherProperty"": ""{anotherProperty}""
}}";

			var command = JsonSerializer.Deserialize<TestCommand>(json,
				TransactoSerializerOptions.CommandSerializerOptions(typeof(TestBusinessTransaction)));

			Assert.Equal(property, command.Property);
			Assert.Equal(anotherProperty, command.AnotherProperty);

			var transaction = Assert.IsType<TestBusinessTransaction>(command.BusinessTransaction);
			Assert.Equal(transactionId, transaction.TransactionId);
		}

		[Fact]
		public void DeserializationWhenBusinessTransactionTypeDoesNotExist() {
			var property = Guid.NewGuid().ToString();
			var anotherProperty = Guid.NewGuid().ToString();
			var transactionId = Guid.NewGuid();
			var json = $@"{{
	""property"": ""{property}"",
	""businessTransaction"": {{
		""doesNotExist"": {JsonSerializer.Serialize(new TestBusinessTransaction {TransactionId = transactionId, Version = 1})}
	}},
    ""anotherProperty"": ""{anotherProperty}""
}}";

			var command = JsonSerializer.Deserialize<TestCommand>(json,
				TransactoSerializerOptions.CommandSerializerOptions(typeof(TestBusinessTransaction)));

			Assert.Equal(property, command.Property);
			Assert.Equal(anotherProperty, command.AnotherProperty);

			Assert.Null(command.BusinessTransaction);
		}

		[Fact]
		public void Serialization() {
			var sut = TransactoSerializerOptions.CommandSerializerOptions(typeof(TestBusinessTransaction));

			var testBusinessTransaction = new TestBusinessTransaction {
				Version = 1,
				TransactionId = Guid.NewGuid()
			};
			var dto = new TestCommand {
				Property = Guid.NewGuid().ToString(),
				AnotherProperty = Guid.NewGuid().ToString(),
				BusinessTransaction = testBusinessTransaction
			};

			var copy = JsonSerializer.Deserialize<TestCommand>(JsonSerializer.Serialize(dto, sut), sut);

			Assert.Equal(dto.Property, copy.Property);
			Assert.Equal(dto.AnotherProperty, copy.AnotherProperty);
			var transaction = Assert.IsType<TestBusinessTransaction>(dto.BusinessTransaction);
			Assert.Equal(testBusinessTransaction.TransactionId, transaction.TransactionId);
		}

		private class TestCommand {
			public string AnotherProperty { get; set; }
			public string Property { get; set; }
			public IBusinessTransaction BusinessTransaction { get; set; }
		}
	}

	internal class TestBusinessTransaction : IBusinessTransaction {
		public Guid TransactionId { get; set; }

		public GeneralLedgerEntry GetGeneralLedgerEntry(PeriodIdentifier period, DateTimeOffset createdOn) {
			var entry = GeneralLedgerEntry.Create(new GeneralLedgerEntryIdentifier(TransactionId),
				new GeneralLedgerEntryNumber(), period, createdOn);

			entry.ApplyTransaction(this);

			return entry;
		}

		public IEnumerable<object> GetAdditionalChanges() {
			yield return this;
		}

		public int? Version { get; set; }
	}
}

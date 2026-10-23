using System.Text.Json;
using NodaTime;
using Transacto.Domain;
using Transacto.Framework.Http;
using Transacto.Messages;

namespace Transacto.Infrastructure;

public class CommandSerializerOptionTests {
	public void Deserialization() {
		var property = Guid.NewGuid().ToString();
		var anotherProperty = Guid.NewGuid().ToString();
		var transactionId = Guid.NewGuid();
		var json = $$"""
		             {
		             	"property": "{{property}}",
		             	"businessTransaction": {
		             		"testBusinessTransaction": {{JsonSerializer.Serialize(new TestBusinessTransaction { TransactionId = transactionId }, new JsonSerializerOptions {
					             WriteIndented = true })}}
		             	},
		                 "anotherProperty": "{{anotherProperty}}"
		             }
		             """;

		var command = JsonSerializer.Deserialize<TestCommand>(json,
			TransactoHttpSerializerOptions.BusinessTransactions(typeof(TestBusinessTransaction)))!;

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
			TransactoHttpSerializerOptions.BusinessTransactions(typeof(TestBusinessTransaction)))!;

		Assert.Equal(property, command.Property);
		Assert.Equal(anotherProperty, command.AnotherProperty);

		Assert.Null(command.BusinessTransaction);
	}

	public void Serialization() {
		var sut = TransactoHttpSerializerOptions.BusinessTransactions(typeof(TestBusinessTransaction));

		var testBusinessTransaction = new TestBusinessTransaction {
			TransactionId = Guid.NewGuid()
		};
		var dto = new TestCommand {
			Property = Guid.NewGuid().ToString(),
			AnotherProperty = Guid.NewGuid().ToString(),
			BusinessTransaction = testBusinessTransaction
		};

		var json = JsonSerializer.Serialize(dto, sut);
		var copy = JsonSerializer.Deserialize<TestCommand>(json, sut)!;

		Assert.Equal(dto.Property, copy.Property);
		Assert.Equal(dto.AnotherProperty, copy.AnotherProperty);
		var transaction = Assert.IsType<TestBusinessTransaction>(dto.BusinessTransaction);
		Assert.Equal(testBusinessTransaction.TransactionId, transaction.TransactionId);
	}

	public void PostGeneralLedgerEntryJournalEntryRoundTripsThroughCommandOptions() {
		var entry = new JournalEntry {
			Description = "Opening balance",
			Items = [
				new JournalEntry.Item(1000, 10m, JournalEntry.Type.Debit),
				new JournalEntry.Item(3000, 10m, JournalEntry.Type.Credit)
			]
		};
		var command = new PostGeneralLedgerEntry {
			GeneralLedgerEntryId = Guid.NewGuid(),
			Period = "202610",
			CreatedOn = new LocalDate(2026, 10, 23),
			BusinessTransaction = entry
		};

		var json = JsonSerializer.Serialize(command, TransactoHttpSerializerOptions.Commands);

		using var document = JsonDocument.Parse(json);
		Assert.Equal(2, document.RootElement.GetProperty("document").GetProperty("journalEntry")
			.GetProperty("items").GetArrayLength());

		var copy = Assert.IsType<PostGeneralLedgerEntry>(JsonSerializer.Deserialize<PostGeneralLedgerEntry>(json,
			TransactoHttpSerializerOptions.BusinessTransactions(typeof(JournalEntry))));
		var journalEntry = Assert.IsType<JournalEntry>(copy.BusinessTransaction);
		Assert.Equal(entry.Description, journalEntry.Description);
		Assert.Equal(entry.Items, journalEntry.Items);
	}

	private class TestCommand {
		public string AnotherProperty { get; set; } = null!;
		public string Property { get; set; } = null!;
		public IBusinessTransaction BusinessTransaction { get; set; } = null!;
	}

	internal class TestBusinessTransaction : IBusinessTransaction {
		public Guid TransactionId { get; set; }

		public GeneralLedgerEntrySequenceNumber? SequenceNumber { get; set; }
		public static GeneralLedgerEntryNumberPrefix Prefix { get; } = new("test");

		public GeneralLedgerEntryNumber? ReferenceNumber =>
			SequenceNumber.HasValue ? new GeneralLedgerEntryNumber(Prefix, SequenceNumber.Value) : null;

		public IEnumerable<object> GetTransactionItems(ChartOfAccounts chartOfAccounts) => [];
	}
}

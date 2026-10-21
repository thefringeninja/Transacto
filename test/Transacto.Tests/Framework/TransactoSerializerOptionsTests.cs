using System.Text.Json;
using Transacto.Domain;

namespace Transacto.Framework;

public class TransactoSerializerOptionsTests {
	public void JournalEntryRoundTripsThroughMessagesOptions() {
		var sequenceNumber = new GeneralLedgerEntrySequenceNumber(7);
		var entry = new JournalEntry {
			Description = "Opening balance",
			Items = [
				new JournalEntry.Item(1000, 10m, JournalEntry.Type.Debit),
				new JournalEntry.Item(3000, 10m, JournalEntry.Type.Credit)
			],
			SequenceNumber = sequenceNumber
		};

		var bytes = JsonSerializer.SerializeToUtf8Bytes(entry, typeof(JournalEntry),
			TransactoSerializerOptions.Messages);

		using var document = JsonDocument.Parse(bytes);
		var root = document.RootElement;

		Assert.Equal("Opening balance", root.GetProperty("description").GetString());
		Assert.Equal(7, root.GetProperty("sequenceNumber").GetInt32());
		Assert.Equal(2, root.GetProperty("items").GetArrayLength());

		var copy = Assert.IsType<JournalEntry>(
			JsonSerializer.Deserialize(bytes, typeof(JournalEntry), TransactoSerializerOptions.Messages));

		Assert.Equal(entry.Description, copy.Description);
		Assert.Equal(entry.Items, copy.Items);
		Assert.Equal(sequenceNumber, copy.SequenceNumber);
	}
}

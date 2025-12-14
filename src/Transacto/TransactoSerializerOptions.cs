using System.Text.Json;
using System.Text.Json.Serialization;
using Transacto.Domain;

namespace Transacto;

public static class TransactoSerializerOptions {
	public static readonly JsonSerializerOptions Events = new() {
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		PropertyNameCaseInsensitive = true,
		IncludeFields = true,
		Converters = {
			new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
			new MoneyConverter(),
			new PercentageConverter(),
			new AccountNumberConverter(),
			new GeneralLedgerEntrySequenceNumberConverter(),
			new GeneralLedgerEntryNumberConverter()
		}
	};

	private class MoneyConverter : JsonConverter<Money> {
		public override Money Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
			new(reader.GetDecimal());

		public override void Write(Utf8JsonWriter writer, Money value, JsonSerializerOptions options) =>
			writer.WriteNumberValue(value.ToDecimal());
	}

	private class PercentageConverter : JsonConverter<Percentage> {
		public override Percentage Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
			new(reader.GetDecimal());

		public override void Write(Utf8JsonWriter writer, Percentage value, JsonSerializerOptions options) {
			writer.WriteNumberValue(value.ToDecimal());
		}
	}

	private class AccountNumberConverter : JsonConverter<AccountNumber> {
		public override AccountNumber Read(ref Utf8JsonReader reader, Type typeToConvert,
			JsonSerializerOptions options) {
			return new(reader.GetInt32());
		}

		public override void Write(Utf8JsonWriter writer, AccountNumber value, JsonSerializerOptions options) {
			writer.WriteNumberValue(value.ToInt32());
		}
	}

	private class GeneralLedgerEntrySequenceNumberConverter : JsonConverter<GeneralLedgerEntrySequenceNumber> {
		public override GeneralLedgerEntrySequenceNumber Read(ref Utf8JsonReader reader, Type typeToConvert,
			JsonSerializerOptions options) {
			return new(reader.GetInt32());
		}

		public override void Write(Utf8JsonWriter writer, GeneralLedgerEntrySequenceNumber value,
			JsonSerializerOptions options) {
			writer.WriteNumberValue(value.ToInt32());
		}
	}

	private class GeneralLedgerEntryNumberConverter : JsonConverter<GeneralLedgerEntryNumber> {
		public override GeneralLedgerEntryNumber Read(ref Utf8JsonReader reader, Type typeToConvert,
			JsonSerializerOptions options) => GeneralLedgerEntryNumber.Parse(reader.GetString()!);

		public override void Write(Utf8JsonWriter writer, GeneralLedgerEntryNumber value,
			JsonSerializerOptions options) {
			writer.WriteStringValue(value.ToString());
		}
	}
}

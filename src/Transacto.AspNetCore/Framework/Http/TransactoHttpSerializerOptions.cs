using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Hallo.Serialization;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;
using NodaTime.Text;
using Transacto.Domain;

namespace Transacto.Framework.Http;

public static class TransactoHttpSerializerOptions {
	public static readonly JsonSerializerOptions HalJsonSerializerOptions = new() {
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		PropertyNameCaseInsensitive = true,
		IncludeFields = true,
		NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
		Converters = {
			new LinksConverter(),
			new HalRepresentationConverter(),
			new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
			new NodaPatternConverter<LocalDate>(LocalDatePattern.Iso),
			new NodaPatternConverter<Period>(PeriodPattern.NormalizingIso),
			new PatternConverter<LocalDatePattern, LocalDate>(LocalDatePattern.CreateWithInvariantCulture,
				p => p.PatternText)
		},
	};

	private class PatternConverter<T, T2>(Func<string, T> factory, Func<T, string> getPattern)
		: JsonConverter<T> where T : class, IPattern<T2> {
		public override T? Read(ref Utf8JsonReader reader, Type typeToConvert,
			JsonSerializerOptions options) {
			var pattern = reader.GetString();
			return pattern is null ? null : factory(pattern);
		}

		public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options) {
			writer.WriteStringValue(getPattern(value));
		}
	}

	public class LocalDateConverter : JsonConverter<LocalDate> {
		private readonly JsonConverter<LocalDate> _inner = new NodaPatternConverter<LocalDate>(LocalDatePattern.Iso);

		public override LocalDate Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
			return _inner.Read(ref reader, typeToConvert, options);
		}

		public override void Write(Utf8JsonWriter writer, LocalDate value, JsonSerializerOptions options) {
			_inner.Write(writer, value, options);
		}
	}

	public class PeriodConverter : JsonConverter<Period> {
		private readonly JsonConverter<Period> _inner = new NodaPatternConverter<Period>(PeriodPattern.NormalizingIso);

		public override Period? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
			return _inner.Read(ref reader, typeToConvert, options);
		}

		public override void Write(Utf8JsonWriter writer, Period value, JsonSerializerOptions options) {
			_inner.Write(writer, value, options);
		}
	}

	// Client-side options must match the server's converters (NodaTime, Money, ledger numbers, ...) or
	// requests are rejected with 400. The generated context covers the Transacto message types; reflection
	// covers plugin business transactions such as JournalEntry that the context does not know.
	private static JsonSerializerOptions FromMessages() => new(TransactoSerializerOptions.Messages) {
		TypeInfoResolver = JsonTypeInfoResolver.Combine(
			TransactoSerializerOptions.Messages.TypeInfoResolver!,
			new DefaultJsonTypeInfoResolver())
	};

	public static readonly JsonSerializerOptions Commands = FromMessages();

	public static JsonSerializerOptions BusinessTransactions(params Type[] businessTransactionTypes) {
		var options = FromMessages();
		options.Converters.Insert(0, new BusinessTransactionConverter(businessTransactionTypes));
		return options;
	}

	private class BusinessTransactionConverter(params Type[] transactionTypes) : JsonConverter<object?> {
		private readonly Dictionary<string, Type> _transactionTypes =
			transactionTypes.ToDictionary(GetBusinessTransactionPropertyName);

		private static string GetBusinessTransactionPropertyName(Type type) =>
			char.ToLower(type.Name[0]) + type.Name[1..];

		// Only the declared interface is handled here. Concrete business transaction types must fall through to
		// default serialization, otherwise the converter would recurse into itself.
		public override bool CanConvert(Type typeToConvert) => typeToConvert == typeof(IBusinessTransaction);

		public override object? Read(ref Utf8JsonReader reader, Type typeToConvert,
			JsonSerializerOptions options) {
			// Wrapper shape: { "<transactionTypeName>": { ...transaction... } }
			if (reader.TokenType != JsonTokenType.StartObject || !reader.Read() ||
			    reader.TokenType != JsonTokenType.PropertyName) {
				throw new JsonException();
			}

			var typeName = reader.GetString()!;

			// Move onto the transaction's value.
			reader.Read();

			object? businessTransaction = null;

			if (_transactionTypes.TryGetValue(typeName, out var type) &&
			    typeof(IBusinessTransaction).IsAssignableFrom(type)) {
				businessTransaction = JsonSerializer.Deserialize(ref reader, type, options);
			} else {
				reader.Skip();
			}

			// The reader now sits on the transaction's last token. Advance to the wrapper's closing brace.
			if (!reader.Read() || reader.TokenType != JsonTokenType.EndObject) {
				throw new JsonException();
			}

			return businessTransaction;
		}

		public override void Write(Utf8JsonWriter writer, object? value,
			JsonSerializerOptions options) {
			if (value == null) {
				writer.WriteNullValue();
				return;
			}

			writer.WriteStartObject();

			writer.WritePropertyName(GetBusinessTransactionPropertyName(value.GetType()));

			JsonSerializer.Serialize(writer, value, value.GetType(), options);

			writer.WriteEndObject();
		}
	}
}

using System.Text.Json;
using System.Text.Json.Serialization;
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

	public static JsonSerializerOptions BusinessTransactions(params Type[] businessTransactionTypes) =>
		new() {
			IncludeFields = true,
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			PropertyNameCaseInsensitive = true,
			Converters = {
				new BusinessTransactionConverter(businessTransactionTypes),
				new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
			},
		};

	public static readonly JsonSerializerOptions Commands = new() {
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		PropertyNameCaseInsensitive = true,
		IncludeFields = true,
		Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
	};

	private class BusinessTransactionConverter(params Type[] transactionTypes) : JsonConverter<object?> {
		private readonly Dictionary<string, Type> _transactionTypes =
			transactionTypes.ToDictionary(GetBusinessTransactionPropertyName);

		private static string GetBusinessTransactionPropertyName(Type type) =>
			char.ToLower(type.Name[0]) + type.Name[1..];

		public override bool CanConvert(Type typeToConvert) =>
			typeof(IBusinessTransaction).IsAssignableFrom(typeToConvert);

		public override object? Read(ref Utf8JsonReader reader, Type typeToConvert,
			JsonSerializerOptions options) {
			if (reader.TokenType != JsonTokenType.StartObject || !reader.Read()) {
				throw new JsonException();
			}

			var typeName = reader.GetString()!;

			if (!_transactionTypes.TryGetValue(typeName, out var type) ||
			    !typeof(IBusinessTransaction).IsAssignableFrom(type)) {
				reader.Skip();

				reader.Read();

				return null;
			}

			var businessTransaction = JsonSerializer.Deserialize(ref reader, type, options)!;
			reader.Read();

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

			using var document = JsonDocument.Parse(
				JsonSerializer.SerializeToUtf8Bytes(value, value.GetType(), TransactoSerializerOptions.Events));

			document.RootElement.WriteTo(writer);

			writer.WriteEndObject();
		}
	}
}

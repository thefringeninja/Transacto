using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Transacto.Domain;

namespace Transacto {
	public static class TransactoSerializerOptions {
		public static readonly JsonSerializerOptions Events = new JsonSerializerOptions {
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			PropertyNameCaseInsensitive = true
		};

		public static JsonSerializerOptions BusinessTransactions(params Type[] businessTransactionTypes) =>
			new JsonSerializerOptions {
				PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
				PropertyNameCaseInsensitive = true,
				Converters = {new BusinessTransactionConverter(businessTransactionTypes)}
			};

		public static readonly JsonSerializerOptions Commands = new JsonSerializerOptions {
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			PropertyNameCaseInsensitive = true
		};

		private class BusinessTransactionConverter : JsonConverter<IBusinessTransaction?> {
			private readonly IDictionary<string, Type> _transactionTypes;

			public BusinessTransactionConverter(params Type[] transactionTypes) {
				_transactionTypes = transactionTypes.ToDictionary(GetBusinessTransactionPropertyName);
			}

			private static string GetBusinessTransactionPropertyName(Type type) =>
				char.ToLower(type.Name[0]) + type.Name[1..];

			public override IBusinessTransaction? Read(ref Utf8JsonReader reader, Type typeToConvert,
				JsonSerializerOptions options) {
				if (reader.TokenType != JsonTokenType.StartObject || !reader.Read()) {
					throw new JsonException();
				}

				var typeName = reader.GetString();

				if (!_transactionTypes.TryGetValue(typeName, out var type) ||
				    !typeof(IBusinessTransaction).IsAssignableFrom(type)) {
					reader.Skip();

					reader.Read();

					return null;
				}

				var businessTransaction = (IBusinessTransaction)JsonSerializer.Deserialize(ref reader, type, options);
				reader.Read();

				return businessTransaction;
			}

			public override void Write(Utf8JsonWriter writer, IBusinessTransaction? value,
				JsonSerializerOptions options) {
				if (value == null) {
					writer.WriteNullValue();
					return;
				}

				writer.WriteStartObject();

				writer.WritePropertyName(GetBusinessTransactionPropertyName(value.GetType()));

				using var document = JsonDocument.Parse(
					JsonSerializer.SerializeToUtf8Bytes(value, value.GetType(), Events));

				document.RootElement.WriteTo(writer);

				writer.WriteEndObject();
			}
		}
	}
}

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
			new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
		}
	};
}

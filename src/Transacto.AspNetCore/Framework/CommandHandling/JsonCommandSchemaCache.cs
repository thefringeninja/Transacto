using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Nodes;
using Corvus.Json.JsonSchema.Draft201909;

namespace Transacto.Framework.CommandHandling;

public class JsonCommandSchemaCache(params Type[] commandTypes) {
	private readonly ConcurrentDictionary<Type, JsonElement> _scripts = [];

	public Type[] CommandTypes { get; } = commandTypes;
	public JsonElement this[Type commandType] => GetSchema(commandType);

	private JsonElement GetSchema(Type commandType) {
		if (!CommandTypes.Contains(commandType)) {
			throw new InvalidOperationException();
		}

		return _scripts.GetOrAdd(commandType,
			static key => {
				using var stream = key.Assembly.GetManifestResourceStream(key, $"{key.Name}.schema.json") ??
				                   throw new Exception($"Embedded resource {key} not found. BUG!");

				var schema = JsonNode.Parse(stream) ?? throw new Exception($"Embedded resource {key} was null. BUG!");
				schema[Schema.JsonPropertyNames.Id] = key.Name;

				return JsonDocument.Parse(schema.ToJsonString()).RootElement;
			});
	}
}

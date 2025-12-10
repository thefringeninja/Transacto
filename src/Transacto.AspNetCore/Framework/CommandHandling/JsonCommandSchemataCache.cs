using System.Collections.Concurrent;
using System.Text.Json;
using Corvus.Json.JsonSchema.Draft201909;
using Inflector;

namespace Transacto.Framework.CommandHandling;

public class JsonCommandSchemataCache(params Type[] commandTypes) {
	private readonly ConcurrentDictionary<Type, JsonCommandSchema> _schemata = [];
	private readonly ConcurrentDictionary<string, Type> _schemaTypeByName = [];

	public Type[] CommandTypes { get; } = commandTypes;
	public JsonCommandSchema this[Type commandType] => GetJsonCommandSchema(commandType);

	public JsonCommandSchema this[string commandType] {
		get {
			var type = _schemaTypeByName.GetOrAdd(commandType, static (key, commandTypes) => {
				var name = key.Underscore().Pascalize();
				return commandTypes.Single(t => t.Name == name);
			}, CommandTypes);
			return GetJsonCommandSchema(type);
		}
	}

	private JsonCommandSchema GetJsonCommandSchema(Type commandType) =>
		!CommandTypes.Contains(commandType)
			? throw new InvalidOperationException()
			: _schemata.GetOrAdd(commandType, static key => new JsonCommandSchema(GetSchema(key), GetUiSchema(key)));

	private static JsonElement? GetUiSchema(Type key) {
		var stream = key.Assembly.GetManifestResourceStream(key, $"{key.Name}.uischema.json");
		if (stream is null) {
			return null;
		}

		using (stream) {
			return JsonDocument.Parse(stream).RootElement;
		}
	}

	private static Schema GetSchema(Type key) {
		using var stream = key.Assembly.GetManifestResourceStream(key, $"{key.Name}.schema.json") ??
		                   throw new Exception($"Embedded resource {key} not found. BUG!");

		return Schema.Parse(stream).WithId(new Core.IdEntity(key.Name));
	}
}

public record JsonCommandSchema(Schema Schema, JsonElement? UiSchema);

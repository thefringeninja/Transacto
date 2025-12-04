using System.Collections.Concurrent;
using System.Text.Json;
using Corvus.Json.JsonSchema.Draft201909;

namespace Transacto.Framework.CommandHandling;

public class JsonCommandSchemaCache(params Type[] commandTypes) {
	private readonly ConcurrentDictionary<Type, Schema> _scripts = [];

	public Type[] CommandTypes { get; } = commandTypes;
	public Schema this[Type commandType] => GetSchema(commandType);
	
	private Schema GetSchema(Type commandType) {
		if (!CommandTypes.Contains(commandType)) {
			throw new InvalidOperationException();
		}

		return _scripts.GetOrAdd(commandType,
			key => {
				using var stream = key.Assembly.GetManifestResourceStream(key, $"{key.Name}.schema.json");
				if (stream == null) {
					throw new Exception($"Embedded resource {key} not found. BUG!");
				}

				return Schema.Parse(stream);
			});
	}
}

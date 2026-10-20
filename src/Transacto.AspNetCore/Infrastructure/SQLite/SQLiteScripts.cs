using System.Collections.Concurrent;
using System.Reflection;

namespace Transacto.Infrastructure.SQLite;

public abstract class SQLiteScripts {
	private Assembly s_assembly => GetType().Assembly;

	private readonly ConcurrentDictionary<string, string> _scripts
		= new();

	public string this[Type eventType] => GetScript(eventType.Name);

	public const string CreateSchema = """
	                                   CREATE TABLE IF NOT EXISTS checkpoints(
	                                   	`commit` INTEGER NOT NULL,
	                                   	`prepare` INTEGER NOT NULL,
	                                   	`id` INTEGER NOT NULL PRIMARY KEY
	                                   )
	                                   """;

	public const string ReadCheckpoint = "SELECT `commit`, `prepare` FROM checkpoints";

	public const string WriteCheckpoint =
		"REPLACE INTO checkpoints (`commit`, `prepare`, id) VALUES (@commit, @prepare, 1)";

	private string GetScript(string name) => _scripts.GetOrAdd(name,
		key => {
			using var stream = s_assembly.GetManifestResourceStream(GetType(), $"{key}.sql");
			if (stream == null) {
				throw new Exception($"Embedded resource, {key}, not found. BUG!");
			}

			using StreamReader reader = new(stream);

			return reader.ReadToEnd();
		});
}

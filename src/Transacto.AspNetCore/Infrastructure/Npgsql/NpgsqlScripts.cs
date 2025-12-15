using System.Collections.Concurrent;
using System.Reflection;

namespace Transacto.Infrastructure.Npgsql;

public abstract class NpgsqlScripts {
	private Assembly s_assembly => GetType().Assembly;

	private readonly ConcurrentDictionary<string, string> _scripts
		= new();

	public string this[Type eventType] => GetScript(eventType.Name);

	public const string CreateCheckpoints =
		"""
		CREATE TABLE IF NOT EXISTS checkpoints
		(
			projection	VARCHAR(256) 	NOT NULL,
			prepare		BIGINT 			NOT NULL,
			commit		BIGINT 			NOT NULL,
			CONSTRAINT	pk_checkpoints PRIMARY KEY (projection)
		);

		""";

	public const string ReadCheckpoint =
		"""
		SELECT commit, prepare FROM checkpoints WHERE projection = @projection
		""";

	public const string WriteCheckpoint =
		"""
		INSERT INTO checkpoints (commit, prepare, projection) VALUES (@commit, @prepare, @projection)
		ON CONFLICT (projection) DO
		UPDATE SET commit = EXCLUDED.commit, prepare = EXCLUDED.prepare
		""";

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

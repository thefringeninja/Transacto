using System;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection;

namespace Transacto {
	public abstract class NpgsqlScripts {
		private Assembly s_assembly => GetType().Assembly;

		private readonly ConcurrentDictionary<string, string> _scripts
			= new ConcurrentDictionary<string, string>();

		public string Schema { get; set; } = null!;

		public string this[Type eventType] => GetScript(eventType.Name);

		public const string ReadCheckpoint = "SELECT commit, prepare FROM checkpoints WHERE projection = @projection";

		public const string WriteCheckpoint = @"
			INSERT INTO checkpoints (commit, prepare, projection) VALUES (@commit, @prepare, @projection)
			ON CONFLICT DO
			UPDATE SET commit = @commit, prepare = @prepare WHERE projection = @projection";

		private string GetScript(string name) => _scripts.GetOrAdd(name,
			key => {
				using var stream = s_assembly.GetManifestResourceStream(GetType(), $"{key}.sql");
				if (stream == null) {
					throw new Exception($"Embedded resource, {key}, not found. BUG!");
				}

				using StreamReader reader = new StreamReader(stream);

				return reader
					.ReadToEnd()
					.Replace("__schema__", Schema);
			});
	}
}

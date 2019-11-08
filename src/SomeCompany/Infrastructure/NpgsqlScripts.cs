using System;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection;

namespace SomeCompany.Infrastructure {
    public abstract class NpgsqlScripts {
        private static readonly Assembly s_assembly = typeof(NpgsqlScripts)
            .GetTypeInfo()
            .Assembly;
        private readonly ConcurrentDictionary<Type, string> _scripts
            = new ConcurrentDictionary<Type, string>();

        public string Schema { get; }

        public string this[Type eventType] => GetScript(eventType);

        protected NpgsqlScripts(string schema)
        {
            if (schema == null) {
                throw new ArgumentNullException(nameof(schema));
            }
            Schema = schema;
        }

        private string GetScript(Type eventType) => _scripts.GetOrAdd(eventType,
            key => {
                using var stream = s_assembly.GetManifestResourceStream(GetType(), $"{key.Name}.sql");
                if(stream == null)
                {
                    throw new Exception($"Embedded resource, {key.Name}, not found. BUG!");
                }

                using StreamReader reader = new StreamReader(stream);

                return reader
                    .ReadToEnd()
                    .Replace("__schema__", Schema);
            });
    }
}

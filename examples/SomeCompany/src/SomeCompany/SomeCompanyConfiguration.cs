using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.CommandLine;

#nullable enable
namespace SomeCompany {
    internal class SomeCompanyConfiguration {
        private readonly ConfigurationData _configuration;

        public string ConnectionString => _configuration.ConnectionString;

        public SomeCompanyConfiguration(string[] args, IDictionary environment) {
            _configuration = new ConfigurationData(
                new ConfigurationBuilder()
                    .Add(new CommandLineSource(args))
                    .Add(new EnvironmentVariablesSource(environment))
                    .Build());
        }

        private class ConfigurationData {
            private readonly IConfigurationRoot _configurationRoot;

            public string ConnectionString => _configurationRoot.GetValue<string>(nameof(ConnectionString));

            public ConfigurationData(IConfigurationRoot configurationRoot) {
                _configurationRoot = configurationRoot;
            }
        }

        private class CommandLineSource : IConfigurationSource {
            private readonly IEnumerable<string> _args;

            public CommandLineSource(IEnumerable<string> args) {
                if (args == null) {
                    throw new ArgumentNullException(nameof(args));
                }

                _args = args;
            }

            public IConfigurationProvider Build(IConfigurationBuilder builder)
                => new CommandLine(_args);
        }

        private class CommandLine : CommandLineConfigurationProvider {
            public CommandLine(
                IEnumerable<string> args,
                IDictionary<string, string>? switchMappings = null)
                : base(args, switchMappings) {
            }

            public override void Load() {
                base.Load();

                Data = Data.Keys.ToDictionary(Computerize, x => Data[x]);
            }
        }

        private class EnvironmentVariablesSource : IConfigurationSource {
            private readonly IDictionary _environment;
            public string Prefix { get; set; } = "SC";

            public EnvironmentVariablesSource(
                IDictionary environment) {
                if (environment == null) {
                    throw new ArgumentNullException(nameof(environment));
                }

                _environment = environment;
            }

            public IConfigurationProvider Build(IConfigurationBuilder builder)
                => new EnvironmentVariables(Prefix, _environment);
        }

        private class EnvironmentVariables : ConfigurationProvider {
            private readonly IDictionary _environment;
            private readonly string _prefix;

            public EnvironmentVariables(
                string prefix,
                IDictionary environment) {
                if (environment == null) {
                    throw new ArgumentNullException(nameof(environment));
                }

                _prefix = $"{prefix}_";
                _environment = environment;
            }

            public override void Load() {
                Data = (from entry in _environment.OfType<DictionaryEntry>()
                        let key = (string)entry.Key
                        where key.StartsWith(_prefix)
                        select new {
                            key = Computerize(key.Remove(0, _prefix.Length)),
                            value = (string)entry.Value!
                        })
                    .ToDictionary(x => x.key, x => x.value);
            }
        }

        private static string Computerize(string value) =>
            string.Join(
                string.Empty,
                (value?.Replace("-", "_").ToLowerInvariant()
                 ?? string.Empty).Split('_')
                .Select(x => new string(x.Select((c, i) => i == 0 ? char.ToUpper(c) : c).ToArray())));
    }
}

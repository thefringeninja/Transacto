using System.Collections;
using Microsoft.Extensions.Configuration.CommandLine;

namespace SomeCompany;

internal class SomeCompanyConfiguration {
	private readonly ConfigurationData _configuration;

	public string ConnectionString => _configuration.ConnectionString ?? "esdb://localhost:2113/?tls=false";

	public SomeCompanyConfiguration(string[] args, IDictionary environment) {
		_configuration = new ConfigurationData(
			new ConfigurationBuilder()
				.Add(new CommandLineSource(args))
				.Add(new EnvironmentVariablesSource(environment))
				.Build());
	}

	private class ConfigurationData {
		private readonly IConfigurationRoot _configurationRoot;

		public string? ConnectionString => _configurationRoot.GetValue<string>(nameof(ConnectionString));

		public ConfigurationData(IConfigurationRoot configurationRoot) {
			_configurationRoot = configurationRoot;
		}
	}

	private class CommandLineSource : IConfigurationSource {
		private readonly IEnumerable<string> _args;

		public CommandLineSource(IEnumerable<string> args) {
			_args = args;
		}

		public IConfigurationProvider Build(IConfigurationBuilder builder) => new CommandLine(_args);
	}

	private class CommandLine : CommandLineConfigurationProvider {
		public CommandLine(IEnumerable<string> args) : base(args) {
		}

		public override void Load() {
			base.Load();

			Data = Data.Keys.ToDictionary(Computerize, x => Data[x]);
		}
	}

	private class EnvironmentVariablesSource : IConfigurationSource {
		private readonly IDictionary _environment;
		public string Prefix { get; set; } = "SC";

		public EnvironmentVariablesSource(IDictionary environment) {
			_environment = environment;
		}

		public IConfigurationProvider Build(IConfigurationBuilder builder)
			=> new EnvironmentVariables(Prefix, _environment);
	}

	private class EnvironmentVariables : ConfigurationProvider {
		private readonly IDictionary _environment;
		private readonly string _prefix;

		public EnvironmentVariables(string prefix, IDictionary environment) {
			_prefix = $"{prefix}_";
			_environment = environment;
		}

		public override void Load() {
			foreach (var (k, v) in _environment.OfType<DictionaryEntry>()) {
				var key = (string)k;
				if (!key.StartsWith(_prefix)) {
					continue;
				}

				Data[Computerize(key.Remove(0, _prefix.Length))] = (string?)v;
			}
		}
	}

	private static string Computerize(string value) =>
		string.Join(
			string.Empty,
			value.Replace("-", "_").ToLowerInvariant().Split('_')
			.Select(x => new string(x.Select((c, i) => i == 0 ? char.ToUpper(c) : c).ToArray())));
}

using System.Collections;
using Microsoft.Extensions.Configuration.CommandLine;

namespace SomeCompany;

internal class SomeCompanyConfiguration(string[] args, IDictionary environment) {
	private readonly ConfigurationData _configuration = new(
		new ConfigurationBuilder()
			.Add(new CommandLineSource(args))
			.Add(new EnvironmentVariablesSource(environment))
			.Build());

	public string EventStore => _configuration.EventStore ?? "esdb://localhost:2113/?tls=false";

	public string Postgres => _configuration.Postgres ?? "Host=localhost";

	public string SqlStreamStore => _configuration.SqlStreamStore ?? "http://localhost:5002/";

	public int TransactionsPerMonth => _configuration.TransactionsPerMonth ?? 100;
	
	public bool Seed => _configuration.Seed ?? false;

	public LogLevel LogLevel => _configuration.LogLevel ?? LogLevel.Information;
	
	private class ConfigurationData(IConfigurationRoot configurationRoot) {
		public string? EventStore => configurationRoot.GetValue<string>(nameof(EventStore));
		public string? Postgres => configurationRoot.GetValue<string>(nameof(Postgres));
		public string? SqlStreamStore => configurationRoot.GetValue<string>(nameof(SqlStreamStore));
		public int? TransactionsPerMonth => configurationRoot.GetValue<int?>(nameof(TransactionsPerMonth));
		public bool? Seed => configurationRoot.GetValue<bool?>(nameof(Seed));
		public LogLevel? LogLevel => configurationRoot.GetValue<LogLevel?>(nameof(LogLevel));
	}

	private class CommandLineSource(IEnumerable<string> args) : IConfigurationSource {
		public IConfigurationProvider Build(IConfigurationBuilder builder) => new CommandLine(args);
	}

	private class CommandLine(IEnumerable<string> args) : CommandLineConfigurationProvider(args) {
		public override void Load() {
			base.Load();

			Data = Data.Keys.ToDictionary(Computerize, x => Data[x]);
		}
	}

	private class EnvironmentVariablesSource(IDictionary environment) : IConfigurationSource {
		public string Prefix { get; set; } = "SC";

		public IConfigurationProvider Build(IConfigurationBuilder builder)
			=> new EnvironmentVariables(Prefix, environment);
	}

	private class EnvironmentVariables(string prefix, IDictionary environment) : ConfigurationProvider {
		private readonly string _prefix = $"{prefix}_";

		public override void Load() {
			foreach (var (k, v) in environment.OfType<DictionaryEntry>()) {
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

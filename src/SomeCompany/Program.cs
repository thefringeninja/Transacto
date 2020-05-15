using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventStore.Client;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Serilog;
using SqlStreamStore;
using Transaction.AspNetCore;

namespace SomeCompany {
	internal class Program : IDisposable {
		private readonly CancellationTokenSource _exitedSource;
		private readonly IStreamStore _streamStore;
		private readonly IHostBuilder _hostBuilder;

		private Program(SomeCompanyConfiguration configuration) {
			Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;
			Log.Logger = new LoggerConfiguration()
				//.MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
				.Enrich.FromLogContext()
				.WriteTo.Console(
					outputTemplate:
					"[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
				.CreateLogger();

			_exitedSource = new CancellationTokenSource();

			var connectionStringBuilder = new NpgsqlConnectionStringBuilder(configuration.ConnectionString);

			_streamStore = new InMemoryStreamStore();

			_hostBuilder = Host.CreateDefaultBuilder()
				.ConfigureLogging(builder => builder.AddSerilog())
				.ConfigureWebHost(builder => builder
					.UseKestrel()
					.ConfigureServices(services => services
						.AddEventStoreClient()
						.AddSingleton<Func<NpgsqlConnection>>(() =>
							new NpgsqlConnection(connectionStringBuilder.ConnectionString))
						.AddSingleton(_streamStore))
					.UseStartup(new Startup(GetPlugins())));

			static IPlugin[] GetPlugins() =>
				typeof(Startup).Assembly.GetExportedTypes().Where(typeof(IPlugin).IsAssignableFrom)
					.Select(t => (IPlugin)Activator.CreateInstance(t)!)
					.ToArray();
		}

		private async Task<int> Run() {
			try {
				await _hostBuilder.RunConsoleAsync(lifetime => lifetime.SuppressStatusMessages = true);
				return 0;
			} catch (Exception ex) {
				Log.Fatal(ex, "Host terminated unexpectedly.");
				return 1;
			} finally {
				Log.CloseAndFlush();
			}
		}

		public static async Task<int> Main(string[] args) {
			using var program = new Program(new SomeCompanyConfiguration(args, Environment.GetEnvironmentVariables()));

			return await program.Run();
		}

		public void Dispose() {
			_exitedSource.Dispose();
			_streamStore.Dispose();
		}
	}
}

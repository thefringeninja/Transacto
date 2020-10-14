using System;
using System.Globalization;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Serilog;
using SqlStreamStore;
using Transacto;

namespace SomeCompany {
	internal class Program : IDisposable {
		private readonly CancellationTokenSource _exitedSource;
		private readonly IHostBuilder _hostBuilder;

		private Program(SomeCompanyConfiguration configuration) {
			DefaultTypeMap.MatchNamesWithUnderscores = true;
			Inflector.Inflector.SetDefaultCultureFunc = () => new CultureInfo("en-US");
			Log.Logger = new LoggerConfiguration()
				.MinimumLevel.Verbose()
				//.MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
				.Enrich.FromLogContext()
				.WriteTo.Console(
					outputTemplate:
					"[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
				.CreateLogger();

			_exitedSource = new CancellationTokenSource();

			_hostBuilder = TransactoHost.Build(new ServiceCollection()
				.AddEventStoreClient(settings => settings.CreateHttpMessageHandler = () => new SocketsHttpHandler {
					SslOptions = {
						RemoteCertificateValidationCallback = delegate { return true; }
					}
				})
				.AddSingleton<IStreamStore>(new HttpClientSqlStreamStore(new HttpClientSqlStreamStoreSettings {
					BaseAddress = new UriBuilder {
						Port = 5002
					}.Uri
				}))
				.AddSingleton(new NpgsqlConnectionStringBuilder(configuration.ConnectionString))
				.BuildServiceProvider());
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
		}
	}
}

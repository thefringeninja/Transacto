using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using Serilog;
using Serilog.Events;
using SqlStreamStore;

namespace SomeCompany {
    internal class Program : IDisposable {
        private readonly IWebHost _host;
        private readonly CancellationTokenSource _exitedSource;
        private readonly IStreamStore _streamStore;

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

            _host = new WebHostBuilder()
                .SuppressStatusMessages(true)
                .UseKestrel()
                .UseStartup(new Startup(_streamStore, connectionStringBuilder))
                .UseSerilog()
                .Build();
            _streamStore = new InMemoryStreamStore();

            Console.CancelKeyPress += (_, e) => _exitedSource.Cancel();
        }

        private async Task<int> Run() {
            try {
                await _host.RunAsync(_exitedSource.Token);
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
            _host.Dispose();
            _streamStore.Dispose();
        }
    }
}

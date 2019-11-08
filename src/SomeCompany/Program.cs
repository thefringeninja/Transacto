using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Npgsql;
using Serilog;
using Serilog.Events;

namespace SomeCompany {
    internal class Program : IDisposable {
        private readonly IWebHost _host;
        private readonly CancellationTokenSource _exitedSource;

        private Program(SomeCompanyConfiguration configuration) {
            Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Is(LogEventLevel.Information)
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
                .UseStartup(new Startup(connectionStringBuilder))
                .UseSerilog()
                .Build();

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
        }
    }
}

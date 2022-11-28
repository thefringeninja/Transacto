using System.Globalization;
using Dapper;
using EventStore.Client;
using Npgsql;
using Serilog;
using SomeCompany;
using SqlStreamStore;
using Transacto;

var configuration = new SomeCompanyConfiguration(args, Environment.GetEnvironmentVariables());
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

var builder = WebApplication.CreateBuilder().AddTransacto(
	new EventStoreClient(EventStoreClientSettings.Create("esdb://localhost:2113/?tls=false")),
	new NpgsqlConnectionStringBuilder(configuration.ConnectionString),
	new HttpClientSqlStreamStore(new() { BaseAddress = new UriBuilder { Port = 5002 }.Uri }));

var app = builder.Build();
app.UseTransacto();

try {
	await app.RunAsync();
	return 0;
} catch (Exception ex) {
	Log.Fatal(ex, "Host terminated unexpectedly.");
	return 1;
} finally {
	Log.CloseAndFlush();
}

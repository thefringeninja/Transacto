using System.Globalization;
using Dapper;
using EventStore.Client;
using Npgsql;
using Serilog;
using Serilog.Events;
using SomeCompany;
using SqlStreamStore;
using Transacto;

var configuration = new SomeCompanyConfiguration(args, Environment.GetEnvironmentVariables());
DefaultTypeMap.MatchNamesWithUnderscores = true;
Inflector.Inflector.SetDefaultCultureFunc = () => new CultureInfo("en-US");

var builder = WebApplication.CreateBuilder(args);
builder.Logging
	.ClearProviders()
	.AddSerilog(new LoggerConfiguration()
		.MinimumLevel.Debug()
		.MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
		.Enrich.FromLogContext()
		.WriteTo.Console(
			outputTemplate:
			"[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
		.CreateLogger());
builder.Services.AddTransacto(
	new EventStoreClient(EventStoreClientSettings.Create(configuration.EventStore)),
	new NpgsqlConnectionStringBuilder(configuration.Postgres),
	new HttpClientSqlStreamStore(new() { BaseAddress = new Uri(configuration.SqlStreamStore) }));

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

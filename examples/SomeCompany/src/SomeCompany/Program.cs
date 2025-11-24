using System.Globalization;
using System.Text.Json;
using Dapper;
using Ductus.FluentDocker.Builders;
using Ductus.FluentDocker.Common;
using Ductus.FluentDocker.Model.Common;
using Ductus.FluentDocker.Services;
using Ductus.FluentDocker.Services.Extensions;
using KurrentDB.Client;
using Microsoft.CodeAnalysis.Emit;
using NodaTime;
using NodaTime.Text;
using Npgsql;
using Polly;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;
using SomeCompany;
using SqlStreamStore;
using Transacto;
using Transacto.Domain;
using Transacto.Framework;
using Transacto.Messages;

var configuration = new SomeCompanyConfiguration(args, Environment.GetEnvironmentVariables());
DefaultTypeMap.MatchNamesWithUnderscores = true;
Inflector.Inflector.SetDefaultCultureFunc = () => new CultureInfo("en-US");

var logProvider = new SerilogLoggerProvider(new LoggerConfiguration()
	.MinimumLevel.Verbose()
	//.MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
	.Enrich.FromLogContext()
	.WriteTo.Console(
		outputTemplate:
		"[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj} {Properties:j}{NewLine}{Exception}")
	.CreateLogger());

var dockerHostUri = new DockerUri(Environment.GetEnvironmentVariable("DOCKER_HOST") ??
                                  "unix:///run/user/1000/docker.sock");

var containerHostBuilder = new Builder()
	.UseHost()
	.FromUri(dockerHostUri);

var containerHost = containerHostBuilder.Build();

using var eventStore = BuildAndStartContainer(
		$"transacto-some-company-kurrent",
		"ghcr.io/kurrent-io/eventstore:23.10.8-alpine",
		2113, 2113,
		("EVENTSTORE_ENABLE_ATOM_PUB_OVER_HTTP", true), ("EVENTSTORE_INSECURE", true))
	.WaitForMessageInLogs("IS LEADER... SPARTA!", 10000);


var builder = WebApplication.CreateBuilder(args);

builder.Logging
	.ClearProviders()
	.AddProvider(logProvider);

builder.Services.AddTransacto(
	new KurrentDBClient(KurrentDBClientSettings.Create(configuration.EventStore)),
	new NpgsqlConnectionStringBuilder(configuration.Postgres),
	SQLiteConnectionStringBuilderFactories.OnDisk,
	new HttpClientSqlStreamStore(new() { BaseAddress = new Uri(configuration.SqlStreamStore) }),
	() => logProvider);

var app = builder.Build();

var messageTypeMapper = app.Services.GetRequiredService<IMessageTypeMapper>();

app.UseTransacto();

_ = Seed();

try {
	await app.RunAsync();
	return 0;
} catch (Exception ex) {
	Log.Fatal(ex, "Host terminated unexpectedly.");
	return 1;
} finally {
	Log.CloseAndFlush();
}

async Task Seed() {
	var client = app.Services.GetRequiredService<KurrentDBClient>();

	await client.AppendToStreamAsync(GeneralLedger.Identifier, StreamState.NoStream, Array.ConvertAll([
		new GeneralLedgerOpened {
			OpenedOn = LocalDatePattern.Iso.Format(new LocalDate(2025, 11, 1))
		}
	], ToEventData));

	await client.AppendToStreamAsync(ChartOfAccounts.Identifier, StreamState.NoStream, Array.ConvertAll([
		(object)new ChartOfAccountsConfigured {
			AccountNumberWidth = 4
		},
		new AccountDefined {
			AccountName = "Bank",
			AccountNumber = 1000,
			AffectsCash = true,
			CashFlowCategory = DefineAccountCashFlowCategory.Operations
		},
		new AccountDefined {
			AccountName = "A/R",
			AccountNumber = 1300,
			AffectsCash = false,
			CashFlowCategory = DefineAccountCashFlowCategory.Operations
		},
		new AccountDefined {
			AccountName = "Retained Earnings",
			AccountNumber = 3900,
			AffectsCash = false,
			CashFlowCategory = DefineAccountCashFlowCategory.Finance
		},
		new AccountDefined {
			AccountName = "Income",
			AccountNumber = 4000,
			AffectsCash = false,
			CashFlowCategory = DefineAccountCashFlowCategory.Operations
		},
		new AccountDefined {
			AccountName = "COGS",
			AccountNumber = 5000,
			AffectsCash = false,
			CashFlowCategory = DefineAccountCashFlowCategory.Operations
		}
	], ToEventData));
}

IContainerService BuildAndStartContainer(string name, string image, int hostPort, int containerPort,
	params (string key, object value)[] environment) {
	return Policy.Handle<FluentDockerException>(ex =>
			ex.Message.Contains("You have to remove (or rename) that container to be able to reuse that name"))
		.Retry(5, StopContainer)
		.Execute(() => containerHostBuilder
			.UseContainer()
			.WithName(name)
			.UseImage(image)
			.DeleteIfExists()
			.ExposePort(hostPort, containerPort)
			.WithEnvironment(Array.ConvertAll(environment, input => $"{input.key}={input.value}"))
			.Build()
			.Start());

	void StopContainer(Exception ex, int attempt) => containerHost
		.GetRunningContainers()
		.FirstOrDefault(x => x.Name == name)?.Stop();
}

EventData ToEventData(object e) => new EventData(Uuid.NewUuid(), messageTypeMapper.Map(e.GetType()),
	JsonSerializer.SerializeToUtf8Bytes(e, e.GetType(), TransactoSerializerOptions.Events));

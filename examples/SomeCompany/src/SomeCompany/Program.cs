using System.Collections.Immutable;
using System.Globalization;
using System.Text.Json;
using Dapper;
using Ductus.FluentDocker.Builders;
using Ductus.FluentDocker.Common;
using Ductus.FluentDocker.Model.Common;
using Ductus.FluentDocker.Services;
using Ductus.FluentDocker.Services.Extensions;
using KurrentDB.Client;
using NodaTime;
using Npgsql;
using Polly;
using Serilog;
using Serilog.Extensions.Logging;
using SomeCompany;
using SomeCompany.Sales;
using SqlStreamStore;
using Transacto;
using Transacto.Domain;
using Transacto.Framework;
using Transacto.Messages;

var configuration = new SomeCompanyConfiguration(args, Environment.GetEnvironmentVariables());
DefaultTypeMap.MatchNamesWithUnderscores = true;
Inflector.Inflector.SetDefaultCultureFunc = () => new CultureInfo("en-US");

var openedOn = new LocalDate(2025, 11, 1);
var period = AccountingPeriod.Open(openedOn);
var logProvider = new SerilogLoggerProvider(new LoggerConfiguration()
	.MinimumLevel.Verbose()
	//.MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
	.Enrich.FromLogContext()
	.WriteTo.Console(
		applyThemeToRedirectedOutput: true,
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
builder.Environment.EnvironmentName = Environments.Development;

builder.Logging
	.ClearProviders()
	.AddProvider(logProvider);

builder.Services
	.AddTransacto(
		new KurrentDBClient(KurrentDBClientSettings.Create(configuration.EventStore)),
		new NpgsqlConnectionStringBuilder(configuration.Postgres),
		SQLiteConnectionStringBuilderFactories.OnDisk,
		new HttpClientSqlStreamStore(new() { BaseAddress = new Uri(configuration.SqlStreamStore) }),
		() => logProvider,
		builder.Environment,
		[new Sales()]);

var app = builder.Build();

app.UseTransacto();

var generalLedger = GeneralLedger.Open(openedOn);
var chartOfAccounts = ChartOfAccounts.Factory();
chartOfAccounts.DefineAccount(new AccountName("A/R"), new AccountNumber(1100), CashFlowCategory.Operations, false);
chartOfAccounts.DefineAccount(new AccountName("Bank"), new AccountNumber(1000), CashFlowCategory.Operations, true);
chartOfAccounts.DefineAccount(new AccountName("Inventory"), new AccountNumber(1200), CashFlowCategory.Investment,
	false);
chartOfAccounts.DefineAccount(new AccountName("Retained Earnings"), new AccountNumber(3900), CashFlowCategory.Finance,
	false);
chartOfAccounts.DefineAccount(new AccountName("COGS"), new AccountNumber(5000), CashFlowCategory.Operations, false);
chartOfAccounts.DefineAccount(new AccountName("Sales"), new AccountNumber(4000), CashFlowCategory.Operations, false);
chartOfAccounts.DefineAccount(new AccountName("Shipping"), new AccountNumber(4050), CashFlowCategory.Operations, false);
chartOfAccounts.DefineAccount(new AccountName("Sales Tax Payable"), new AccountNumber(2300),
	CashFlowCategory.Operations, false);

var client = app.Services.GetRequiredService<KurrentDBClient>();
var messageTypeMapper = app.Services.GetRequiredService<IMessageTypeMapper>();
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
	try {
		var events = OpenBooks()
			.Concat(DefineChartOfAccounts())
			.Concat(CreateAndPayInvoices());

		await foreach (var (streamName, @event) in events) {
			await client.AppendToStreamAsync(streamName, StreamState.Any, [ToEventData(@event)]);
		}
	} catch (Exception ex) {
		app.Logger.LogError(ex, "Seeding failed");
	}
}

async IAsyncEnumerable<(string, object)> CreateAndPayInvoices() {
	await Task.Yield();
	int invoiceNumber = 0;
	for (var month = 0; month < 12; month++) {
		generalLedger.MarkChangesAsCommitted();
		var generalLedgerEntries = new List<GeneralLedgerEntry>();
		for (var i = 0; i < configuration.TransactionsPerMonth; i++) {
			var invoice = new Invoice(++invoiceNumber, new(100m), new(7.5m), new(45m), Percentage.Of(8.25m));

			var invoiceEntry = generalLedger.Create(new(Guid.NewGuid()), invoice, new("invoice"),
				openedOn.PlusMonths(month).AtMidnight(), _ => false);
			invoiceEntry.Post();

			foreach (var change in GetChanged(invoiceEntry)) {
				yield return change;
			}

			generalLedgerEntries.Add(invoiceEntry);

			var paymentEntry = generalLedger.Create(new(Guid.NewGuid()), new Payment(invoiceNumber, invoice.Total),
				new("payment"), openedOn.PlusMonths(month).AtMidnight(), _ => false);
			paymentEntry.Post();
			foreach (var change in GetChanged(paymentEntry)) {
				yield return change;
			}

			generalLedgerEntries.Add(paymentEntry);
		}

		await using var subscription = client.SubscribeToStream(GeneralLedger.Identifier, FromStream.End);

		var closingGeneralLedgerEntryIdentifier = new GeneralLedgerEntryIdentifier(Guid.NewGuid());

		generalLedger.BeginClosingPeriod(chartOfAccounts.Get<EquityAccount>(new(3900)),
			closingGeneralLedgerEntryIdentifier, [..generalLedgerEntries.Select(x => x.Identifier)],
			openedOn.PlusMonths(month + 1).PlusDays(-1).AtMidnight());

		foreach (var e in generalLedger.GetChanges()) {
			yield return (GeneralLedger.Identifier, e);
		}

		await foreach (var message in subscription.Messages) {
			if (message is not StreamMessage.Event (var resolvedEvent)) {
				continue;
			}

			if (!messageTypeMapper.TryMap(resolvedEvent.Event.EventType, out var type)) {
				continue;
			}

			var e = JsonSerializer.Deserialize(resolvedEvent.Event.Data.Span, type, TransactoSerializerOptions.Events);
			generalLedger.ReadFromHistory(e!);

			if (resolvedEvent is { Event.EventType: nameof(AccountingPeriodClosed) }) {
				break;
			}
		}

		period = period.Next();
	}

	yield break;

	static IEnumerable<(string, object)> GetChanged(GeneralLedgerEntry entry) {
		var streamName = GeneralLedgerEntry.FormatStreamIdentifier(entry.Identifier);
		foreach (var e in entry.GetChanges()) {
			yield return (streamName, e);
		}
	}
}

async IAsyncEnumerable<(string streamName, object @event)> DefineChartOfAccounts() {
	await Task.Yield();
	foreach (var e in chartOfAccounts.GetChanges()) {
		yield return (ChartOfAccounts.Identifier, e);
	}
}


async IAsyncEnumerable<(string, object)> OpenBooks() {
	await Task.Yield();
	foreach (var e in generalLedger.GetChanges()) {
		yield return (GeneralLedger.Identifier, e);
	}
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

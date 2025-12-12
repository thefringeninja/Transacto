using System.Globalization;
using AngleSharp.Html.Parser;
using Dapper;
using Ductus.FluentDocker.Builders;
using Ductus.FluentDocker.Common;
using Ductus.FluentDocker.Model.Common;
using Ductus.FluentDocker.Services;
using Ductus.FluentDocker.Services.Extensions;
using KurrentDB.Client;
using Microsoft.IO;
using NodaTime;
using Npgsql;
using Polly;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;
using SomeCompany;
using SomeCompany.PurchaseOrders;
using SomeCompany.Sales;
using SqlStreamStore;
using Transacto;
using Transacto.Domain;
using Transacto.Framework.CommandHandling;
using Transacto.Messages;
using Transacto.Plugins;

var configuration = new SomeCompanyConfiguration(args, Environment.GetEnvironmentVariables());
DefaultTypeMap.MatchNamesWithUnderscores = true;
Inflector.Inflector.SetDefaultCultureFunc = () => new CultureInfo("en-US");

var openedOn = new LocalDate(2025, 11, 1);
var period = AccountingPeriod.Open(openedOn);
var logProvider = new SerilogLoggerProvider(new LoggerConfiguration()
	.MinimumLevel.Is(configuration.LogLevel switch {
		LogLevel.Trace => LogEventLevel.Verbose,
		LogLevel.Debug => LogEventLevel.Debug,
		LogLevel.Warning => LogEventLevel.Warning,
		LogLevel.Error => LogEventLevel.Error,
		LogLevel.Critical => LogEventLevel.Fatal,
		_ => LogEventLevel.Information
	})
	.MinimumLevel.Override("Microsoft", LogEventLevel.Information)
	.Enrich.FromLogContext()
	.WriteTo.Seq("http://localhost:5341")
	.WriteTo.Console(
		applyThemeToRedirectedOutput: true,
		restrictedToMinimumLevel: LogEventLevel.Information,
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
		"transacto-some-company-kurrent",
		"eventstore/eventstore:lts",
		configuration.EventStore.Port, 2113,
		("EVENTSTORE_ENABLE_ATOM_PUB_OVER_HTTP", true), ("EVENTSTORE_INSECURE", true))
	.WaitForMessageInLogs("IS LEADER... SPARTA!", 10000);

using var sqlStreamStore = BuildAndStartContainer(
		"transacto-some-company-sss",
		"sql-stream-store:latest",
		configuration.SqlStreamStore.Port, 80)
	.WaitForMessageInLogs("SqlStreamStore.HAL.SqlStreamStoreHalMiddleware", 10000);

using var postgres = BuildAndStartContainer(
		"transacto-some-company-pgsql",
		"postgres:latest",
		configuration.Postgres.Port, 5342,
		("POSTGRES_PASSWORD", configuration.Postgres.Password ?? ""))
	.WaitForMessageInLogs("database system is ready to accept connections", 10000);

var builder = WebApplication.CreateBuilder(args);
builder.Environment.EnvironmentName = Environments.Development;

builder.Logging
	.ClearProviders()
	.SetMinimumLevel(configuration.LogLevel)
	.AddProvider(logProvider);

var salesPlugin = new Sales();
var purchaseOrderPlugin = new PurchaseOrders();

builder.AddTransacto(
	new KurrentDBClient(KurrentDBClientSettings.Create(configuration.EventStore.ToString())),
	configuration.Postgres,
	SQLiteConnectionStringBuilderFactories.OnDisk,
	new HttpClientSqlStreamStore(new() { BaseAddress = configuration.SqlStreamStore }),
	[salesPlugin, purchaseOrderPlugin]);

var app = builder.Build();

var manager = new RecyclableMemoryStreamManager();
var parser = new HtmlParser();

// app.Use(async (HttpContext context, Func<Task> next) => {
// 	using var buffer = manager.GetStream();
// 	var responseStream = context.Response.Body;
//
// 	try {
//
// 		context.Response.Body = buffer;
//
// 		await next();
//
// 		buffer.Position = 0;
//
// 		context.Response.Body = responseStream;
//
// 		if (context.Response.ContentType != "text/html") {
// 			await buffer.CopyToAsync(context.Response.Body);
//
// 			return;
// 		}
//
// 		var document = await parser.ParseDocumentAsync(buffer);
// 		var html = document.ToHtml(new PrettyMarkupFormatter {
// 			Indentation = "\t",
// 			NewLine = "\n"
// 		});
//
// 		await context.Response.WriteAsync(html);
// 	} finally {
// 		context.Response.Body = responseStream;
// 	}
// });
app.UseTransacto(salesPlugin, purchaseOrderPlugin);

var client = app.Services.GetRequiredService<KurrentDBClient>();

if (configuration.Seed) {
	_ = Seed();
}

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
	var generalLedger = app.Services.GetRequiredKeyedService<IServiceProvider>(Standard.GeneralLedger)
		.GetRequiredService<CommandDispatcher>();

	var chartOfAccounts = app.Services.GetRequiredKeyedService<IServiceProvider>(Standard.ChartOfAccounts)
		.GetRequiredService<CommandDispatcher>();

	try {
		DeleteReadModels();
		await OpenBooks();
		await DefineChartOfAccounts();
		await CreateAndPayInvoices();
	} catch (Exception ex) {
		app.Logger.LogError(ex, "Seeding failed");
	}

	async Task CreateAndPayInvoices() {
		int invoiceNumber = 0;
		for (var month = 0; month < 12; month++) {
			var ledgerEntryIds = new List<Guid>();
			for (var i = 0; i < configuration.TransactionsPerMonth; i++) {
				var invoice = new Invoice(++invoiceNumber, new(100m), new(7.5m), new(45m), Percentage.Of(8.25m));
				var payment = new Payment(invoiceNumber, invoice.Total);

				var invoiceEntryId = Guid.NewGuid();
				var yearMonth = openedOn.PlusMonths(month).ToYearMonth();
				var invoiceDate = yearMonth
					.OnDayOfMonth(i % yearMonth.Calendar.GetDaysInMonth(yearMonth.Year, yearMonth.Month) + 1)
					.AtMidnight();
				await generalLedger.Handle(new PostGeneralLedgerEntry {
					BusinessTransaction = invoice.ToJournalEntry(),
					CreatedOn = invoiceDate.ToDateTimeUnspecified(),
					GeneralLedgerEntryId = invoiceEntryId,
					Period = period.ToString()
				});
				ledgerEntryIds.Add(invoiceEntryId);

				var paymentEntryId = Guid.NewGuid();
				await generalLedger.Handle(new PostGeneralLedgerEntry {
					BusinessTransaction = payment.ToJournalEntry(),
					CreatedOn = invoiceDate.PlusDays(15).ToDateTimeUnspecified(),
					GeneralLedgerEntryId = paymentEntryId,
					Period = period.ToString()
				});
				ledgerEntryIds.Add(paymentEntryId);
			}

			await using var subscription = client.SubscribeToStream(GeneralLedger.Identifier, FromStream.End);

			var closingGeneralLedgerEntryId = Guid.NewGuid();

			await generalLedger.Handle(new BeginClosingAccountingPeriod {
				ClosingGeneralLedgerEntryId = closingGeneralLedgerEntryId,
				GeneralLedgerEntryIds = [..ledgerEntryIds],
				RetainedEarningsAccountNumber = 3900,
				ClosingOn = openedOn.PlusMonths(month + 1).PlusDays(-1).AtMidnight().ToDateTimeUnspecified()
			});

			await foreach (var message in subscription.Messages) {
				if (message is not StreamMessage.Event (var resolvedEvent)) {
					continue;
				}

				if (resolvedEvent is { Event.EventType: nameof(AccountingPeriodClosed) }) {
					break;
				}
			}

			period = period.Next();
		}
	}

	async Task DefineChartOfAccounts() {
		await chartOfAccounts.Handle(new ConfigureChartOfAccounts {
			AccountNumberWidth = 4
		});
		await chartOfAccounts.Handle(new DefineAccount {
			AccountName = "A/R", AccountNumber = 1100, AffectsCash = false,
			CashFlowCategory = DefineAccountCashFlowCategory.Operations
		});
		await chartOfAccounts.Handle(new DefineAccount {
			AccountName = "Bank", AccountNumber = 1000, AffectsCash = true,
			CashFlowCategory = DefineAccountCashFlowCategory.Operations
		});
		await chartOfAccounts.Handle(new DefineAccount {
			AccountName = "Inventory", AccountNumber = 1200, AffectsCash = false,
			CashFlowCategory = DefineAccountCashFlowCategory.Investment
		});
		await chartOfAccounts.Handle(new DefineAccount {
			AccountName = "Retained Earnings", AccountNumber = 3900, AffectsCash = false,
			CashFlowCategory = DefineAccountCashFlowCategory.Finance
		});
		await chartOfAccounts.Handle(new DefineAccount {
			AccountName = "COGS", AccountNumber = 5000, AffectsCash = false,
			CashFlowCategory = DefineAccountCashFlowCategory.Operations
		});
		await chartOfAccounts.Handle(new DefineAccount {
			AccountName = "Sales", AccountNumber = 4000, AffectsCash = false,
			CashFlowCategory = DefineAccountCashFlowCategory.Operations
		});
		await chartOfAccounts.Handle(new DefineAccount {
			AccountName = "Shipping", AccountNumber = 4050, AffectsCash = false,
			CashFlowCategory = DefineAccountCashFlowCategory.Operations
		});
		await chartOfAccounts.Handle(new DefineAccount {
			AccountName = "Sales Tax Payable", AccountNumber = 2300, AffectsCash = false,
			CashFlowCategory = DefineAccountCashFlowCategory.Operations
		});
	}

	async Task OpenBooks() {
		await generalLedger.Handle(new OpenGeneralLedger {
			OpenedOn = openedOn.AtMidnight().ToDateTimeUnspecified()
		});
	}
}

void DeleteReadModels() {
	foreach (var fi in new DirectoryInfo(app.Environment.ContentRootPath).EnumerateFiles("*.db")) {
		fi.Delete();
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
			.ReuseIfExists()
			.KeepRunning()
			.KeepContainer()
			.ExposePort(hostPort, containerPort)
			.WithEnvironment(Array.ConvertAll(environment, input => $"{input.key}={input.value}"))
			.Build()
			.Start());

	void StopContainer(Exception ex, int attempt) => containerHost
		.GetRunningContainers()
		.FirstOrDefault(x => x.Name == name)?.Stop();
}


internal static class BusinessTransactionExtensions {
	extension(IBusinessTransaction businessTransaction) {
		public JournalEntry ToJournalEntry() {
			return businessTransaction switch {
				JournalEntry entry => entry,
				_ => new JournalEntry {
					Items = [..businessTransaction.Items]
				}
			};
		}

		private IEnumerable<JournalEntry.Item> Items {
			get {
				foreach (var t in businessTransaction.GetTransactionItems()) {
					switch (t) {
						case Debit debit:
							yield return new JournalEntry.Item(debit.AccountNumber.Value, debit.Amount.ToDecimal(),
								JournalEntry.Type.Debit);
							break;
						case Credit credit:
							yield return new JournalEntry.Item(credit.AccountNumber.Value, credit.Amount.ToDecimal(),
								JournalEntry.Type.Credit);
							break;
					}
				}
			}
		}
	}
}

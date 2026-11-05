using System.Data.SQLite;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Grpc.Core;
using KurrentDB.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NodaTime;
using Npgsql;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;
using SqlStreamStore;
using Transacto.Domain;
using Transacto.Framework.Http;
using Transacto.Infrastructure.Npgsql;
using Transacto.Messages;

namespace Transacto.Integration;

public abstract class IntegrationTests : IAsyncDisposable {

	private readonly HttpClientSqlStreamStore _streamStoreClient;
	private readonly WebApplication _app;

	private readonly ComposeContainer _compose;
	private readonly DirectoryInfo _composeDirectory;
	//private readonly IContainerService _streamStore;
	//private readonly IntegrationTestFactory _webApplicationFactory;

	private readonly List<FileInfo> _sqliteDatabases = [];
	protected HttpClient HttpClient { get; }
	protected KurrentDBClient KurrentDBClient { get; }

	static IntegrationTests() {
		Inflector.Inflector.SetDefaultCultureFunc = () => new CultureInfo("en-US");
		DapperMapper.Initialize();
	}

	protected IntegrationTests(TextWriter logOutput, params IPlugin[] plugins) {
		var runId = Convert.ToHexStringLower(Guid.NewGuid().ToByteArray()[..4]);

		// Testcontainers copies the compose file's whole directory into the compose container, so it gets a directory of its own.
		_composeDirectory = Directory.CreateTempSubdirectory($"transacto-it-{runId}-");
		var composeFilePath = Path.Combine(_composeDirectory.FullName, "docker-compose.yml");
		using (var composeResource = typeof(IntegrationTests).Assembly.GetManifestResourceStream(
			       typeof(IntegrationTests), "docker-compose.yml") ??
		       throw new InvalidOperationException("The docker-compose.yml embedded resource is missing.")) {
			using var composeFile = File.Create(composeFilePath);
			composeResource.CopyTo(composeFile);
		}

		_compose = new ComposeBuilder("docker:29.8.1-cli")
			.WithProjectNamePrefix("transacto-it-")
			.WithComposeFile(composeFilePath)
			.WithExposedService("eventstore", 2113,
				Wait.ForUnixContainer().UntilMessageIsLogged("IS LEADER... SPARTA!"))
			.WithExposedService("postgres", 5432,
				Wait.ForUnixContainer().UntilCommandIsCompleted("pg_isready", "-h", "127.0.0.1", "-U", "postgres"))
			.Build();

		_compose.StartAsync().GetAwaiter().GetResult();
		// _streamStore = new Builder()
		// 	.UseContainer()
		// 	.WithName("transacto-sss-test")
		// 	.UseImage("ghcr.io/thefringeninja/sqlstreamstore-server:1.2.0-beta.8-alpine3.9")
		// 	.ReuseIfExists()
		// 	.ExposePort(5000, 80)
		// 	.WaitForHttp("http://localhost:5000", contentType: "application/hal+json")
		// 	.Build();

		var serilogLoggerProvider = new SerilogLoggerProvider(new LoggerConfiguration()
			.MinimumLevel.Verbose()
			.MinimumLevel.Override("KurrentDB", LogEventLevel.Warning)
			.MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
			.MinimumLevel.Override("Grpc", LogEventLevel.Warning)
			.Enrich.FromLogContext()
			.WriteTo.TextWriter(logOutput,
				outputTemplate:
				"[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj} {Properties:j}{NewLine}{Exception}")
			.CreateLogger());

		var eventStoreClientSettings = new KurrentDBClientSettings {
			ConnectivitySettings = {
				Address = new UriBuilder {
					Host = _compose.GetServiceHost("eventstore", 2113),
					Port = _compose.GetServicePort("eventstore", 2113)
				}.Uri,
				Insecure = true
			},
			OperationOptions = {
				ThrowOnAppendFailure = true
			},
			CreateHttpMessageHandler = () => new SocketsHttpHandler {
				SslOptions = {
					RemoteCertificateValidationCallback = delegate {
						return true;
					}
				}
			}
		};
		_streamStoreClient = new HttpClientSqlStreamStore(new HttpClientSqlStreamStoreSettings {
			CreateHttpClient = () => new HttpClient(new SocketsHttpHandler {
				SslOptions = {
					RemoteCertificateValidationCallback = delegate {
						return true;
					}
				}
			}, true)
		});

		var builder = WebApplication.CreateBuilder();
		builder.Logging.ClearProviders().AddProvider(serilogLoggerProvider);
		
		builder.Services.AddTransacto(
			eventStoreClientSettings,
			new NpgsqlConnectionStringBuilder {
				Host = _compose.GetServiceHost("postgres", 5432),
				Port = _compose.GetServicePort("postgres", 5432),
				Username = "postgres",
				Password = "password",
				Database = "postgres"
			},
			SQLiteInTempFolder,
			_streamStoreClient,
			plugins);
		builder.WebHost.UseTestServer();
		_app = builder.Build();

		KurrentDBClient = _app.Services.GetRequiredService<KurrentDBClient>();
		_app.UseTransacto(plugins);
		WaitForEventStore(KurrentDBClient);
		_app.Start();

		var testServer = _app.GetTestServer();

		HttpClient = testServer.CreateClient();
		return;

		// The first gRPC call to a freshly started container can fail with HTTP_1_1_REQUIRED. Left to the app, that
		// surfaces as a 500 from the first command, and as projection hosts backing off before they subscribe.
		// Make that call here, before the app starts, where it is safe to retry.
		static void WaitForEventStore(KurrentDBClient client) {
			for (var attempt = 1;; attempt++) {
				try {
					client.ReadAllAsync(Direction.Backwards, Position.End, 1).FirstOrDefaultAsync().AsTask()
						.GetAwaiter().GetResult();
					return;
				} catch (RpcException) when (attempt < 10) {
					Thread.Sleep(TimeSpan.FromMilliseconds(200 * attempt));
				}
			}
		}

		SQLiteConnectionStringBuilder SQLiteInTempFolder(Type projectionType) {
			var dataSource = Path.Combine(Path.GetTempPath(), $"{projectionType.FullName}-{runId}.db");
			_sqliteDatabases.Add(new FileInfo(dataSource));
			return new SQLiteConnectionStringBuilder {
				JournalMode = SQLiteJournalModeEnum.Wal,
				SyncMode = SynchronizationModes.Off,
				DataSource = dataSource
			};
		}
	}


	protected async IAsyncEnumerable<Account> OpenBooks(LocalDate now) {
		await HttpClient.SendCommand("/chart-of-accounts", new ConfigureChartOfAccounts {
			AccountNumberWidth = 4,
			AccountsReceivable = 1100,
			AccountsPayable = 2000,
			RetainedEarnings = 3030
		}, TransactoHttpSerializerOptions.Commands);
		await foreach (var account in GetChartOfAccounts().OrderBy(_ => Guid.NewGuid())) {
			await HttpClient.SendCommand("/chart-of-accounts", new DefineAccount {
				AccountName = account.AccountName.ToString(),
				AccountNumber = account.AccountNumber.ToInt32(),
				CashFlowCategory = account.CashFlowCategory.ToEnum(),
				AffectsCash = account.AffectsCash
			}, TransactoHttpSerializerOptions.Commands);
			yield return account;
		}

		await HttpClient.SendCommand("/general-ledger", new OpenGeneralLedger {
			OpenedOn = now
		}, TransactoHttpSerializerOptions.Commands);

		await Task.Delay(TimeSpan.FromMilliseconds(500));
	}

	private async IAsyncEnumerable<Account> GetChartOfAccounts() {
		await using var stream =
			typeof(IntegrationTests).Assembly.GetManifestResourceStream(typeof(IntegrationTests),
				"ChartOfAccounts.json") ??
			throw new InvalidOperationException();

		var doc = await JsonDocument.ParseAsync(stream);

		foreach (var account in doc.RootElement.EnumerateArray()) {
			yield return Account.For(
				new(account.GetProperty("accountNumber").GetInt32()),
				new(account.GetProperty("accountName").GetString()!),
				account.GetProperty("cashFlowCategory").GetString() switch {
					"operations" => CashFlowCategory.Operations,
					"investment" => CashFlowCategory.Investment,
					"finance" => CashFlowCategory.Finance,
					_ => CashFlowCategory.None
				},
				account.TryGetProperty("affectsCash", out var affectsCash) && affectsCash.GetBoolean());
		}
	}

	public async ValueTask DisposeAsync() {
		await _app.StopAsync();
		await _app.DisposeAsync();
		await KurrentDBClient.DisposeAsync();
		_streamStoreClient.Dispose();

		_sqliteDatabases.ForEach(fileInfo => fileInfo.Delete());
		//_streamStore.Dispose();
		await _compose.DisposeAsync();
		_composeDirectory.Delete(true);
	}
}

using System.Data.SQLite;
using System.Globalization;
using System.Text.Json;
using Ductus.FluentDocker.Builders;
using Ductus.FluentDocker.Model.Common;
using Ductus.FluentDocker.Services;
using Ductus.FluentDocker.Services.Extensions;
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
using SqlStreamStore;
using Transacto.Domain;
using Transacto.Infrastructure.SQLite;
using Transacto.Messages;

namespace Transacto.Integration;

public abstract class IntegrationTests : IAsyncDisposable {
	private static int s_RunId = -1;
	private static int s_Port = 2113;


	private readonly HttpClientSqlStreamStore _streamStoreClient;
	private readonly WebApplication _app;
	private readonly TestServer _testServer;
	private readonly int _runId = Interlocked.Increment(ref s_RunId);
	private readonly int _eventStorePort = Interlocked.Increment(ref s_Port);

	private readonly IContainerService _eventStore;
	//private readonly IContainerService _streamStore;
	//private readonly IntegrationTestFactory _webApplicationFactory;

	private readonly List<FileInfo> _sqliteDatabases = [];
	protected HttpClient HttpClient { get; }
	protected KurrentDBClient KurrentDBClient { get; }

	static IntegrationTests() {
		Inflector.Inflector.SetDefaultCultureFunc = () => new CultureInfo("en-US");
	}

	protected IntegrationTests() {
		var dockerHost = new DockerUri(Environment.GetEnvironmentVariable("DOCKER_HOST") ??
		                               "unix:///run/user/1000/docker.sock");
		_eventStore = new Builder()
			.UseHost()
			.FromUri(dockerHost)
			.UseContainer()
			.WithName($"transacto-kurrent-test-{_runId}")
			.UseImage("ghcr.io/kurrent-io/eventstore:23.10.8-alpine")
			.DeleteIfExists()
			.ExposePort(_eventStorePort, 2113)
			.WithEnvironment("EVENTSTORE_ENABLE_ATOM_PUB_OVER_HTTP=true", "EVENTSTORE_INSECURE=true")
			.Build()
			.Start()
			.WaitForMessageInLogs("IS LEADER... SPARTA!", 10000);
		// _streamStore = new Builder()
		// 	.UseContainer()
		// 	.WithName("transacto-sss-test")
		// 	.UseImage("ghcr.io/thefringeninja/sqlstreamstore-server:1.2.0-beta.8-alpine3.9")
		// 	.ReuseIfExists()
		// 	.ExposePort(5000, 80)
		// 	.WaitForHttp("http://localhost:5000", contentType: "application/hal+json")
		// 	.Build();

		//_webApplicationFactory = new IntegrationTestFactory();

		KurrentDBClient = new KurrentDBClient(new KurrentDBClientSettings {
			ConnectivitySettings = {
				Address = new UriBuilder {
					Port = _eventStorePort
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
		});
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
		builder.Logging.ClearProviders().AddSerilog(new LoggerConfiguration()
			.MinimumLevel.Information()
			.MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
			.Enrich.FromLogContext()
			.WriteTo.Console(
				outputTemplate:
				"[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
			.CreateLogger());
		builder.Services.AddSingleton<Func<Type, SQLiteConnectionStringBuilder>>(projectionType => {
				var dataSource = Path.Combine(Path.GetTempPath(), $"{projectionType.FullName}-{_runId}.db");
				_sqliteDatabases.Add(new FileInfo(dataSource));
				return new SQLiteConnectionStringBuilder {
					JournalMode = SQLiteJournalModeEnum.Wal,
					SyncMode = SynchronizationModes.Off,
					DataSource = dataSource
				};
			})
			.AddTransacto(KurrentDBClient, new NpgsqlConnectionStringBuilder(), _streamStoreClient);
		builder.WebHost.UseTestServer();
		_app = builder.Build();
		_app.UseTransacto();
		_app.Start();

		_testServer = _app.GetTestServer();

		HttpClient = _testServer.CreateClient();
	}


	protected async IAsyncEnumerable<Account> OpenBooks(LocalDateTime now) {
		await HttpClient.SendCommand("/chart-of-accounts", new ConfigureChartOfAccounts {
			AccountNumberWidth = 4
		}, TransactoSerializerOptions.Commands);
		await foreach (var account in GetChartOfAccounts().OrderBy(_ => Guid.NewGuid())) {
			await HttpClient.SendCommand("/chart-of-accounts", new DefineAccount {
				AccountName = account.AccountName.ToString(),
				AccountNumber = account.AccountNumber.ToInt32(),
				CashFlowCategory = account.CashFlowCategory.ToEnum(),
				AffectsCash = account.AffectsCash
			}, TransactoSerializerOptions.Commands);
			yield return account;
		}

		await HttpClient.SendCommand("/general-ledger", new OpenGeneralLedger {
			OpenedOn = now.ToDateTimeUnspecified()
		}, TransactoSerializerOptions.Commands);

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
		_eventStore.Dispose();
	}
}

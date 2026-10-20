using System.Data.SQLite;
using System.Globalization;
using System.Net;
using System.Text.Json;
using FluentDocker.Builders;
using FluentDocker.Common;
using FluentDocker.Kernel;
using FluentDocker.Services;
using FluentDocker.Services.Extensions;
using KurrentDB.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NodaTime;
using Npgsql;
using Polly;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;
using SqlStreamStore;
using Transacto.Domain;
using Transacto.Framework.Http;
using Transacto.Messages;

namespace Transacto.Integration;

public abstract class IntegrationTests : IAsyncDisposable {
	private static int s_RunId = -1;
	private static int s_Port = 2113;

	private readonly HttpClientSqlStreamStore _streamStoreClient;
	private readonly WebApplication _app;
	private readonly int _runId = Interlocked.Increment(ref s_RunId);
	private readonly int _eventStorePort = Interlocked.Increment(ref s_Port);

	private readonly IContainerService _eventStore;
	//private readonly IContainerService _streamStore;
	//private readonly IntegrationTestFactory _webApplicationFactory;

	private readonly List<FileInfo> _sqliteDatabases = [];
	private readonly FluentDockerKernel _kernel;
	protected HttpClient HttpClient { get; }
	protected KurrentDBClient KurrentDBClient { get; }

	static IntegrationTests() {
		Inflector.Inflector.SetDefaultCultureFunc = () => new CultureInfo("en-US");
	}

	protected IntegrationTests(TextWriter logOutput) {
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
			new NpgsqlConnectionStringBuilder("Host=localhost;Username=postgres;Password=Password;Port=5432"),
			SQLiteInTempFolder,
			_streamStoreClient);
		builder.WebHost.UseTestServer();
		_app = builder.Build();

		_kernel = FluentDockerKernel.Create(_app.Services.GetRequiredService<ILoggerFactory>())
			.WithDockerCli("docker", d => d.AsDefault())
			.Build();
		
		KurrentDBClient = _app.Services.GetRequiredService<KurrentDBClient>();
		_app.UseTransacto();
		_app.Start();
		
		_eventStore = BuildAndStartContainer(
			$"transacto-kurrent-test-{_runId}",
			"ghcr.io/kurrent-io/eventstore:23.10.8-alpine",
			_eventStorePort, 2113, "IS LEADER... SPARTA!",
			("EVENTSTORE_ENABLE_ATOM_PUB_OVER_HTTP", true), ("EVENTSTORE_INSECURE", true));
		// _streamStore = new Builder()
		// 	.UseContainer()
		// 	.WithName("transacto-sss-test")
		// 	.UseImage("ghcr.io/thefringeninja/sqlstreamstore-server:1.2.0-beta.8-alpine3.9")
		// 	.ReuseIfExists()
		// 	.ExposePort(5000, 80)
		// 	.WaitForHttp("http://localhost:5000", contentType: "application/hal+json")
		// 	.Build();

		//_webApplicationFactory = new IntegrationTestFactory();

		var testServer = _app.GetTestServer();

		HttpClient = testServer.CreateClient();
		return;

		IContainerService BuildAndStartContainer(string name, string image, int hostPort, int containerPort,
			string readyLogMessage, params (string key, object value)[] environment) {
			return Policy.Handle<FluentDockerException>(ex =>
					ex.Message.Contains("You have to remove (or rename) that container to be able to reuse that name"))
				.Retry(5)
				.Execute(() => new Builder()
					.WithinDockerCli("docker", _kernel)
					.UseContainer(c => _ = environment.Aggregate(c.UseImage(image)
						.WithName(name)
						.ExposePort(hostPort, containerPort)
						.WaitForPort($"{containerPort}/tcp")
						.WaitForLogMessage(readyLogMessage, 10000)
						.DestroyIfExists(), (seed, env) => seed.WithEnvironment(env.key, $"{env.value}")))
					.Build()).GetContainer(name);
		}

		SQLiteConnectionStringBuilder SQLiteInTempFolder(Type projectionType) {
			var dataSource = Path.Combine(Path.GetTempPath(), $"{projectionType.FullName}-{_runId}.db");
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
		_eventStore.Dispose();
		await _kernel.DisposeAsync();
	}
}

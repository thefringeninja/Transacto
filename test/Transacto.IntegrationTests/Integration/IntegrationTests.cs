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
	private const string PostgresPassword = "password";

	// One Postgres per process, one database per test. The Testcontainers reaper removes it when the process exits.
	private static readonly Lazy<Task<IContainer>> SharedPostgres = new(StartPostgres);

	// Each stack needs a KurrentDB of its own, so cap how many are alive at once.
	private static readonly SemaphoreSlim Stacks = new(MaxConcurrentStacks());

	private readonly TextWriter _logOutput;
	private readonly IPlugin[] _plugins;
	private readonly string _runId = Convert.ToHexStringLower(Guid.NewGuid().ToByteArray()[..4]);
	private readonly List<FileInfo> _sqliteDatabases = [];

	private HttpClientSqlStreamStore? _streamStoreClient;
	private WebApplication? _app;
	private IContainer? _eventStore;
	private NpgsqlConnectionStringBuilder? _postgresConnectionStringBuilder;
	private bool _holdsStackSlot;

	private HttpClient? _httpClient;
	protected HttpClient HttpClient => _httpClient!;

	private KurrentDBClient? _kurrentDBClient;
	protected KurrentDBClient KurrentDBClient => _kurrentDBClient!;

	static IntegrationTests() {
		Inflector.Inflector.SetDefaultCultureFunc = () => new CultureInfo("en-US");
		DapperMapper.Initialize();
	}

	protected IntegrationTests(TextWriter logOutput, params IPlugin[] plugins) {
		_logOutput = logOutput;
		_plugins = plugins;
	}

	public virtual async Task InitializeAsync() {
		await Stacks.WaitAsync();
		_holdsStackSlot = true;

		var postgresTask = CreatePostgresDatabase();

		_eventStore = new ContainerBuilder("ghcr.io/kurrent-io/eventstore:23.10.8-alpine")
			.WithEnvironment("EVENTSTORE_ENABLE_ATOM_PUB_OVER_HTTP", "True")
			.WithEnvironment("EVENTSTORE_INSECURE", "True")
			.WithPortBinding(2113, true)
			.WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("IS LEADER... SPARTA!"))
			.Build();
		await Task.WhenAll(_eventStore.StartAsync(), postgresTask);

		_postgresConnectionStringBuilder = postgresTask.Result;

		var serilogLoggerProvider = new SerilogLoggerProvider(new LoggerConfiguration()
			.MinimumLevel.Verbose()
			.MinimumLevel.Override("KurrentDB", LogEventLevel.Warning)
			.MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
			.MinimumLevel.Override("Grpc", LogEventLevel.Warning)
			.MinimumLevel.Override("Npgsql", LogEventLevel.Warning)
			.Enrich.FromLogContext()
			.WriteTo.TextWriter(_logOutput,
				outputTemplate:
				"[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj} {Properties:j}{NewLine}{Exception}")
			.CreateLogger());

		var eventStoreClientSettings = new KurrentDBClientSettings {
			ConnectivitySettings = {
				Address = new UriBuilder {
					Host = _eventStore.Hostname,
					Port = _eventStore.GetMappedPublicPort(2113)
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
			_postgresConnectionStringBuilder!,
			SQLiteInTempFolder,
			_streamStoreClient,
			_plugins);
		builder.WebHost.UseTestServer();
		_app = builder.Build();

		_kurrentDBClient = _app.Services.GetRequiredService<KurrentDBClient>();
		_app.UseTransacto(_plugins);
		await WaitForEventStore(_kurrentDBClient);
		await _app.StartAsync();

		_httpClient = _app.GetTestServer().CreateClient();
		return;

		// The first gRPC call to a freshly started container can fail with HTTP_1_1_REQUIRED. Left to the app, that
		// surfaces as a 500 from the first command, and as projection hosts backing off before they subscribe.
		// Make that call here, before the app starts, where it is safe to retry.
		static async Task WaitForEventStore(KurrentDBClient client) {
			for (var attempt = 1;; attempt++) {
				try {
					await client.ReadAllAsync(Direction.Backwards, Position.End, 1).FirstOrDefaultAsync();
					return;
				} catch (RpcException) when (attempt < 10) {
					await Task.Delay(TimeSpan.FromMilliseconds(200 * attempt));
				}
			}
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

	private static int MaxConcurrentStacks() =>
		int.TryParse(Environment.GetEnvironmentVariable("TRANSACTO_IT_PARALLELISM"), out var parallelism) &&
		parallelism > 0
			? parallelism
			: Math.Clamp(Environment.ProcessorCount / 4, 2, 4);

	private static async Task<IContainer> StartPostgres() {
		var container = new ContainerBuilder("postgres:18-alpine")
			.WithEnvironment("POSTGRES_USER", "postgres")
			.WithEnvironment("POSTGRES_PASSWORD", PostgresPassword)
			.WithPortBinding(5432, true)
			.WithWaitStrategy(Wait.ForUnixContainer()
				.UntilCommandIsCompleted("pg_isready", "-h", "127.0.0.1", "-U", "postgres"))
			.Build();
		await container.StartAsync();
		return container;
	}

	private static NpgsqlConnectionStringBuilder PostgresConnection(IContainer container, string database) => new() {
		Host = container.Hostname,
		Port = container.GetMappedPublicPort(5432),
		Username = "postgres",
		Password = PostgresPassword,
		Database = database,
		Pooling = false
	};

	private async Task<NpgsqlConnectionStringBuilder> CreatePostgresDatabase() {
		var container = await SharedPostgres.Value;
		var database = $"it_{_runId}";

		await using var connection = new NpgsqlConnection(PostgresConnection(container, "postgres").ConnectionString);
		await connection.OpenAsync();
		await using var command = connection.CreateCommand();
		command.CommandText = $"CREATE DATABASE {database}";
		await command.ExecuteNonQueryAsync();

		return PostgresConnection(container, database);
	}

	private async Task DropPostgresDatabase() {
		if (_postgresConnectionStringBuilder is null) {
			return;
		}

		NpgsqlConnection.ClearAllPools();
		var container = await SharedPostgres.Value;
		await using var connection = new NpgsqlConnection(PostgresConnection(container, "postgres").ConnectionString);
		await connection.OpenAsync();
		await using var command = connection.CreateCommand();
		command.CommandText = $"DROP DATABASE IF EXISTS it_{_runId} WITH (FORCE)";
		await command.ExecuteNonQueryAsync();
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

	private static async IAsyncEnumerable<Account> GetChartOfAccounts() {
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
		GC.SuppressFinalize(this);

		try {
			if (_app is { } app) {
				await app.StopAsync();
				await app.DisposeAsync();
			}

			if (_kurrentDBClient is { } kurrentDbClient) {
				await kurrentDbClient.DisposeAsync();
			}

			_streamStoreClient?.Dispose();

			_sqliteDatabases.ForEach(fileInfo => fileInfo.Delete());

			await DropPostgresDatabase();

			if (_eventStore is { } eventStore) {
				await eventStore.DisposeAsync();
			}
		} finally {
			if (_holdsStackSlot) {
				_holdsStackSlot = false;
				Stacks.Release();
			}
		}
	}
}

using System;
using System.Data;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EventStore.Grpc;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Projac.Sql;
using Projac.Sql.Executors;
using SomeCompany.BalanceSheet;
using SomeCompany.Infrastructure;
using Transacto.Messages;

namespace SomeCompany {
    public class ProjectionHost : IHostedService {
        private readonly AsyncSqlProjector _projector;
        private readonly EventStoreGrpcClient _client;
        private readonly CancellationTokenSource _stoppedSource;

        public ProjectionHost(string connectionString, params SqlProjectionHandler[] projections) {
            _projector = new AsyncSqlProjector(
                Resolve.WhenEqualToHandlerMessageType(projections),
                new NpgsqlExecutor(() => new NpgsqlConnection(connectionString)));
            _client = new EventStoreGrpcClient(new UriBuilder {
                Port = 2113,
                Scheme = Uri.UriSchemeHttps
            }.Uri);
            _stoppedSource = new CancellationTokenSource();
        }

        public async Task StartAsync(CancellationToken cancellationToken) {
            await _projector.ProjectAsync(new CreateSchema(), cancellationToken);
            var subscription = _client.SubscribeToAll((_, @event, ct) => {
                    var type = typeof(CreditApplied).Assembly.GetType(@event.Event.EventType, false);
                    return type == null
                        ? Task.CompletedTask
                        : _projector.ProjectAsync(JsonSerializer.Deserialize(@event.Event.Data, type), ct);
                },
                userCredentials: new UserCredentials("admin", "changeit"),
                cancellationToken: _stoppedSource.Token);

            _stoppedSource.Token.Register(subscription.Dispose);
        }

        public Task StopAsync(CancellationToken cancellationToken) {
            _stoppedSource.Cancel();
            return Task.CompletedTask;
        }
    }

    public class Startup : IStartup {
        private readonly Func<IDbConnection> _getConnection;
        private readonly string _connectionString;

        public Startup(Func<IDbConnection> getConnection) {
            if (getConnection == null) throw new ArgumentNullException(nameof(getConnection));
            _getConnection = getConnection;
        }

        public Startup(NpgsqlConnectionStringBuilder connectionStringBuilder) {
            _connectionString = connectionStringBuilder.ConnectionString;
            _getConnection = () => new NpgsqlConnection(_connectionString);
        }

        public IServiceProvider ConfigureServices(IServiceCollection services) => services
            .AddRouting()
            .AddSingleton<IHostedService>(new ProjectionHost(_connectionString,
                new BalanceSheetReportProjection("standard_reports")))
            .BuildServiceProvider();

        public void Configure(IApplicationBuilder app) =>
            app.UseRouter(router => router
                .MapMiddlewareRoute(
                    "reports/balance-sheet/{thru}",
                    inner => inner.UseBalanceSheet(new BalanceSheetReportResource(_getConnection,
                        "standard_reports"))));
    }
}

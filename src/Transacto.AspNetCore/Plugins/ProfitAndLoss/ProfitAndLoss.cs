using System.Collections.Immutable;
using System.Data.SQLite;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using NodaTime;
using NodaTime.Text;
using Transacto.Domain;
using Transacto.Framework;
using Transacto.Framework.Http;

namespace Transacto.Plugins.ProfitAndLoss;

public class ProfitAndLoss : IPlugin {
	public string Name => nameof(ProfitAndLoss);

	public void Configure(IEndpointRouteBuilder builder) {
		builder.MapGet("",
			async ([FromQuery] ParseableLocalDate[] startingOn, [FromQuery] ParseablePeriod[] period,
				[FromServices] GetProfitAndLossReportsAsync getProfitAndLossReportsAsync, CancellationToken ct) => {
				var reportRanges = new (LocalDate, Period)[Math.Min(startingOn.Length, period.Length)];

				for (var i = 0; i < reportRanges.Length; i++) {
					reportRanges[i] = (startingOn[i], period[i]);
				}

				var resource = await getProfitAndLossReportsAsync(reportRanges, ct);

				return Results.Extensions.Hal(resource, Checkpoint.None, new ProfitAndLossReportsRepresentation());
			});
	}

	public void ConfigureServices(IServiceCollection services) => services
		.AddSQLiteProjection<ProfitAndLossProjection>()
		.AddSingleton<GetProfitAndLossReportsAsync>(provider => {
			var connectionFactory =
				provider.GetRequiredKeyedService<Func<SQLiteConnection>>(typeof(ProfitAndLossProjection));
			return async (reportRanges, ct) => await ProfitAndLossReportsAsync(connectionFactory, reportRanges, ct);
		});

	private static async Task<ProfitAndLossReports> ProfitAndLossReportsAsync(Func<SQLiteConnection> connectionFactory,
		IEnumerable<(LocalDate startingon, Period period)> reportRanges, CancellationToken ct) {
		const string sql
			= """
			  SELECT p.account_number, SUM(p.balance) 
			  FROM profit_and_loss_daily p
			  WHERE p.date 
			  	BETWEEN @start AND @end
			  GROUP BY p.account_number 
			  """;

		const string accountsSql =
			"""
			SELECT account_number, account_name
			FROM account_names
			""";

		await using var connection = connectionFactory();

		var accounts = await connection
			.QueryUnbufferedAsync<(int AccountNumber, string accountName)>(accountsSql)
			.ToDictionaryAsync(a => a.AccountNumber, a => a.accountName, ct);

		var reports = await reportRanges.ToAsyncEnumerable()
			.SelectAwaitWithCancellation(GetReportAsync)
			.ToArrayAsync(ct);

		return new ProfitAndLossReports(accounts.ToImmutableDictionary(), [..reports]);

		async ValueTask<ProfitAndLossReport> GetReportAsync((LocalDate, Period) q, CancellationToken ct) {
			var (start, period) = q;

			var result = connection.QueryUnbufferedAsync<(int accountNumber, decimal balance)>(
				sql, new {
					start = start.ToDateTimeUnspecified(),
					end = start.Plus(period).Minus(Period.FromDays(1)).ToDateTimeUnspecified()
				});

			return await result.AggregateAsync(ProfitAndLossReport.For(start, period), (report, item) => report with {
				AccountBalances = report.AccountBalances.Add(new(item.accountNumber, item.balance / 100m))
			}, cancellationToken: ct);
		}
	}

	public IEnumerable<Type> MessageTypes => [];
}

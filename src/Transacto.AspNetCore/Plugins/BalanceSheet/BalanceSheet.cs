using System.Collections.Immutable;
using System.Data.SQLite;
using System.Linq;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using NodaTime;
using NodaTime.Text;
using Transacto.Domain;
using Transacto.Framework;
using Transacto.Framework.Http;

namespace Transacto.Plugins.BalanceSheet;

internal class BalanceSheet : IPlugin {
	public string Name { get; } = nameof(BalanceSheet);

	public void Configure(IEndpointRouteBuilder builder) {
		builder.MapGet("", async (
			[FromQuery] ParseableLocalDate?[] thru,
			[FromServices] GetBalanceSheetReportsAsync getBalanceSheetReportsAsync,
			CancellationToken ct) => {
			var resource =
				await getBalanceSheetReportsAsync([..Array.ConvertAll(thru, t => t ?? LocalDate.MaxIsoValue)], ct);

			return Results.Extensions.Hal(resource, Checkpoint.None, new BalanceSheetReportsRepresentation());
		});
	}

	public void ConfigureServices(IServiceCollection services) => services
		.AddSQLiteProjection<BalanceSheetProjection>()
		.AddSingleton<GetBalanceSheetReportsAsync>(provider => {
			var connectionFactory =
				provider.GetRequiredKeyedService<Func<SQLiteConnection>>(typeof(BalanceSheetProjection));

			return async (thru, ct) => await GetBalanceSheetReportsAsync(connectionFactory, thru, ct);
		});

	private static async Task<BalanceSheetReports> GetBalanceSheetReportsAsync(Func<SQLiteConnection> connectionFactory,
		ImmutableArray<LocalDate> thru, CancellationToken ct) {
		const string balancesSql =
			"""
			SELECT b.account_number, SUM(b.balance)
			FROM balance_sheet_daily b
			WHERE b.date <= @thru
			GROUP BY b.account_number
			ORDER BY b.account_number
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

		var reports = await thru.ToAsyncEnumerable()
			.SelectAwaitWithCancellation(GetReportAsync)
			.ToArrayAsync(cancellationToken: ct);

		return new BalanceSheetReports(accounts.ToImmutableDictionary(), [..reports]);

		async ValueTask<BalanceSheetReport> GetReportAsync(LocalDate thru, CancellationToken ct) {
			var result = connection.QueryUnbufferedAsync<(int accountNumber, decimal balance)>(balancesSql, new {
				thru = thru.ToDateTimeUnspecified()
			});

			return await result.AggregateAsync(BalanceSheetReport.For(thru),
				(report, item) => report with {
					AccountBalances = report.AccountBalances.Add(new(item.accountNumber, item.balance / 100m))
				}, cancellationToken: ct);
		}
	}

	public IEnumerable<Type> MessageTypes => [];
}
